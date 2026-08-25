# RecipeApp API

Backend do RecipeApp: uma API REST em .NET 10 para gerenciamento de receitas, com autenticação JWT (email/senha + Google OAuth), chat com IA sobre cada receita (Gemini) e um **assistente agentic** que usa RAG e MCP para responder perguntas sobre toda a coleção de receitas do usuário.

Projeto pessoal construído como vitrine de arquitetura — Clean Architecture, múltiplos serviços deployados de forma independente, e integração de IA com busca semântica e chamada de ferramentas.

## Stack

- **.NET 10** / ASP.NET Core Web API
- **PostgreSQL** com **Entity Framework Core** (Npgsql) e **pgvector** para busca vetorial
- **JWT Bearer** para autenticação + refresh token via cookie HttpOnly
- **Google Auth** (validação de ID Token) para login social
- **Gemini** (`gemini-3.5-flash-lite` para chat, `gemini-embedding-001` para embeddings) via chamadas HTTP diretas à Generative Language API
- **Model Context Protocol** (SDK oficial C#) para o servidor de ferramentas agentic
- **Cloudinary** para hospedagem de imagens (receitas e fotos de perfil, incluindo re-hospedagem de fotos de perfil do Google)
- **Resend** para envio de e-mails (fluxo de reset de senha)
- **Swagger** para documentação interativa da API
- Deploy: **Render** (dois Web Services independentes, via Docker)

## Arquitetura

O projeto é dividido em **cinco projetos** na mesma solution, dois deles publicados como serviços independentes:

```
RecipeApp.slnx
├── RecipeApp.Domain          # Entidades puras, sem dependência de framework
├── RecipeApp.Application     # Regras de negócio: Services, DTOs, interfaces (contratos)
├── RecipeApp.Infrastructure  # Implementações concretas: EF Core, repositórios, integrações externas
├── RecipeApp.Api             # Serviço 1 — Controllers REST, autenticação, Swagger
└── RecipeApp.McpServer       # Serviço 2 — servidor MCP com as ferramentas de busca
```

**Fluxo de dependência (Clean Architecture):** `Api`/`McpServer` → `Application` → `Domain`, com `Infrastructure` implementando as interfaces definidas em `Application`. O `Domain` não depende de nenhuma outra camada — a regra é garantida pelo compilador via `ProjectReference`, não só por convenção.

Cada módulo de negócio (Auth, Recipes, Users, Categories, ChatMessages) segue o mesmo padrão dentro de `Application`/`Infrastructure`: `Service` (regra de negócio) + `Interface` (contrato) + `Repository` (persistência) + `DTOs`.

### Arquitetura Agentic (RAG + MCP)

O diferencial do projeto está no **chat geral** (`/api/chat/general`), que não responde só com contexto fixo — ele decide dinamicamente **como** buscar a informação:

```
Usuário → RecipeApp.Api (client MCP) ⇄ Gemini (decide qual ferramenta chamar, se alguma)
                    ↓
          RecipeApp.McpServer (tools) → PostgreSQL (pgvector + SQL exato)
```

O `RecipeApp.McpServer` é um servidor MCP standalone, com deploy próprio, que expõe duas ferramentas com propósitos distintos:

| Ferramenta | Técnica | Quando é usada |
|---|---|---|
| `search_recipes_semantic` | Busca vetorial (pgvector + cosine distance, com threshold calibrado) | Perguntas fuzzy, tipo "algo leve e rápido" ou "receitas com sabor picante" |
| `search_recipes_excluding_ingredient` | Filtro SQL exato (com suporte a categoria e exclusão de ingrediente) | Perguntas objetivas, tipo "quais receitas não têm queijo" ou "quantas receitas de sobremesa eu tenho" |

O `RecipeApp.Api` atua como **client MCP**: a cada mensagem do chat geral, ele declara as ferramentas disponíveis ao Gemini e deixa o modelo decidir se responde direto ou se precisa consultar dados reais. A implementação suporta múltiplos ciclos de chamada de ferramenta em sequência (loop agentic) antes de formular a resposta final — não é um fluxo fixo de "uma pergunta, uma busca".

**Por que dois serviços separados, no mesmo repositório:** o MCP server roda como processo e deploy independentes (comunicação via HTTP real, protocolo MCP completo), mas compartilha `Domain`/`Infrastructure` com a API principal via referência de projeto — evitando duplicar entidades e configuração de banco entre dois repositórios.

O chat **por receita** (`/api/recipes/{id}/messages`) é mais simples por design: injeta o contexto da receita diretamente no prompt, sem passar por ferramentas — usado quando a pergunta já tem escopo definido (uma receita específica).

## Rodando localmente

### Pré-requisitos

- .NET 10 SDK
- PostgreSQL com a extensão `pgvector` disponível (ex: [Neon](https://neon.tech), que já vem com suporte nativo)

### Setup

```bash
# Restaurar dependências
dotnet restore RecipeApp.slnx

# Aplicar migrations (cria as tabelas e ativa a extensão vector)
dotnet ef database update --project RecipeApp.Infrastructure --startup-project RecipeApp.Api

# Rodar a API
dotnet run --project RecipeApp.Api

# Em outro terminal, rodar o servidor MCP (necessário para o chat geral funcionar)
dotnet run --project RecipeApp.McpServer
```

A API sobe com Swagger disponível em `/swagger` (ambiente Development). O servidor MCP não tem UI — pode ser testado isoladamente com o [MCP Inspector](https://github.com/modelcontextprotocol/inspector).

### Configuração (User Secrets)

O projeto usa **User Secrets** em desenvolvimento (não há segredos versionados nos `appsettings.json`).

**`RecipeApp.Api`:**

```bash
cd RecipeApp.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=recipeapp;Username=postgres;Password=..."
dotnet user-secrets set "Jwt:Secret" "..."
dotnet user-secrets set "Jwt:Issuer" "..."
dotnet user-secrets set "Jwt:Audience" "..."
dotnet user-secrets set "Jwt:AccessTokenMinutes" "15"
dotnet user-secrets set "Jwt:RefreshTokenDays" "30"
dotnet user-secrets set "Google:ClientId" "..."
dotnet user-secrets set "Gemini:ApiKey" "..."
dotnet user-secrets set "Cloudinary:CloudName" "..."
dotnet user-secrets set "Cloudinary:ApiKey" "..."
dotnet user-secrets set "Cloudinary:ApiSecret" "..."
dotnet user-secrets set "Resend:ApiKey" "..."
dotnet user-secrets set "Mcp:ServerUrl" "http://localhost:5100"
```

**`RecipeApp.McpServer`** (usa apenas o essencial — sem auth, e-mail ou imagem):

```bash
cd RecipeApp.McpServer
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=recipeapp;Username=postgres;Password=..."
dotnet user-secrets set "Gemini:ApiKey" "..."
```

Em produção (Render), essas mesmas chaves são configuradas como variáveis de ambiente, uma para cada Web Service.

### Migrations

Ao alterar entidades em `RecipeApp.Domain`, gerar uma nova migration com:

```bash
dotnet ef migrations add NomeDaMigration --project RecipeApp.Infrastructure --startup-project RecipeApp.Api
```

## Autenticação

- Login por e-mail/senha ou Google OAuth (`AuthController`)
- **Access token** (JWT, curta duração) retornado no corpo da resposta
- **Refresh token** (longa duração) setado como cookie HttpOnly — nunca exposto ao JS do cliente
- Fluxo de recuperação de senha por código numérico enviado via e-mail (Resend), com token intermediário de curta duração autorizando a troca de senha

## Endpoints

| Recurso | Rota base | Principais ações |
|---|---|---|
| Auth | `/api/auth` | `register`, `login`, `google`, `refresh`, `logout`, `forgot-password`, `verify-reset-code`, `reset-password` |
| Receitas | `/api/recipes` | `GET`, `GET /{id}`, `POST /create`, `PUT /{id}`, `DELETE /{id}` |
| Categorias | `/api/categories` | `GET` |
| Usuário | `/api/users` | `GET /me`, `PUT /me` |
| Chat da receita | `/api/recipes/{recipeId}/messages` | `GET`, `POST`, `DELETE` |
| Chat geral (agentic) | `/api/chat/general` | `GET`, `POST`, `DELETE` |

Documentação completa e testável em `/swagger` com a API rodando localmente.

## Deploy

- **`RecipeApp.Api`** e **`RecipeApp.McpServer`** rodam como dois Web Services independentes no **Render**, cada um com seu próprio `Dockerfile` e ciclo de deploy.
- O CORS na API principal está configurado para aceitar requisições do domínio do frontend (Vercel), com `AllowCredentials` habilitado.
- **Nota sobre free tier**: ambos os serviços "dormem" após inatividade — a primeira requisição depois de um período ocioso pode levar de 30 a 60 segundos para responder (cold start), potencialmente em cascata se os dois estiverem inativos ao mesmo tempo.

## TODO

- [ ] 2FA (a infraestrutura de código temporário + hash + expiração já existe, reaproveitada do fluxo de reset de senha)
- [ ] Busca de receitas por texto simples (fora do chat)
- [ ] Compartilhar receita via PDF
- [ ] Compartilhar receita via link/app
- [ ] Tool MCP adicional: criar receita diretamente pelo chat

## Decisões de arquitetura

- **Clean Architecture** para manter regras de negócio isoladas de detalhes de infraestrutura (banco, providers externos), facilitando testes e troca de tecnologia em qualquer camada externa.
- **MCP server como serviço separado, não embutido na API**: mesmo tendo hoje um único consumidor (a própria API), rodar como processo/deploy independente significa implementar o protocolo real (descoberta de tools, chamada remota via HTTP) em vez de simular tool calling localmente — e deixa a porta aberta para outros clientes MCP se conectarem no futuro, sem reescrever nada.
- **pgvector em vez de um vector DB dedicado**: para o volume de dados do projeto, Postgres com pgvector é o padrão recomendado atualmente e evita operar um serviço a mais só para embeddings.
- **Duas ferramentas MCP com propósitos deliberadamente diferentes** (busca semântica vs. filtro SQL exato): reflete a limitação real de embeddings para perguntas de correspondência exata (negação, contagem, filtro categórico) — cada ferramenta é usada onde é comprovadamente mais confiável.
- **JWT + Refresh Token via cookie HttpOnly**: o access token curto reduz a janela de exposição em caso de vazamento; o refresh token fica inacessível a scripts no browser, mitigando XSS.
- **User Secrets** em vez de segredos no `appsettings.json`, evitando credenciais versionadas no repositório.
