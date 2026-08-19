# RecipeApp API

Backend do RecipeApp: uma API REST em .NET 10 para gerenciamento de receitas, com autenticação JWT (email/senha + Google OAuth), chat com IA sobre cada receita (Gemini Flash) e upload de imagens via Cloudinary.

Projeto pessoal construído como vitrine de arquitetura e como campo de testes para conceitos de IA aplicada (RAG e MCP estão no roadmap — veja [TODO](#todo)).

## Stack

- **.NET 10** / ASP.NET Core Web API
- **PostgreSQL** com **Entity Framework Core** (Npgsql)
- **JWT Bearer** para autenticação + refresh token via cookie HttpOnly
- **Google Auth** (validação de ID Token) para login social
- **Gemini Flash** (Google Generative Language API) para o chat da receita
- **Cloudinary** para hospedagem de imagens
- **Resend** para envio de e-mails (fluxo de reset de senha)
- **Swagger** para documentação interativa da API
- Deploy: **Render** (via Docker)

## Arquitetura

O projeto segue **Clean Architecture**, organizado em 4 camadas como projetos separados na solution:

```
RecipeApp.slnx
├── RecipeApp.Api             # Controllers, Program.cs, configuração de DI, Swagger, Auth
├── RecipeApp.Application     # Regras de negócio: Services, DTOs, interfaces (contratos)
├── RecipeApp.Domain          # Entidades puras, sem dependência de framework
└── RecipeApp.Infrastructure  # Implementações concretas: EF Core, repositórios, integrações externas
```

**Fluxo de dependência:** `Api` → `Application` → `Domain`, com `Infrastructure` implementando as interfaces definidas em `Application`. O `Domain` não depende de nenhuma outra camada.

Cada módulo de negócio (Auth, Recipes, Users, Categories, ChatMessages) segue o mesmo padrão dentro de `Application`/`Infrastructure`: `Service` (regra de negócio) + `Interface` (contrato) + `Repository` (persistência) + `DTOs`.

## Rodando localmente

### Pré-requisitos

- .NET 10 SDK
- PostgreSQL rodando localmente (ou uma connection string de um Postgres remoto)

### Setup

```bash
# Restaurar dependências
dotnet restore RecipeApp.slnx

# Aplicar migrations (cria as tabelas no banco configurado)
dotnet ef database update --project RecipeApp.Infrastructure --startup-project RecipeApp.Api

# Rodar a API
dotnet run --project RecipeApp.Api
```

A API deve subir com Swagger disponível em `/swagger` (ambiente Development).

### Configuração (User Secrets)

O projeto usa **User Secrets** em desenvolvimento (não há segredos versionados nos `appsettings.json`). Configure com:

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
```

Em produção (Render), essas mesmas chaves são configuradas como variáveis de ambiente.

### Migrations

Ao alterar entidades em `RecipeApp.Domain`, gerar uma nova migration com:

```bash
dotnet ef migrations add NomeDaMigration --project RecipeApp.Infrastructure --startup-project RecipeApp.Api
```

## Autenticação

- Login por e-mail/senha ou Google OAuth (`AuthController`)
- **Access token** (JWT, curta duração) retornado no corpo da resposta
- **Refresh token** (longa duração) setado como cookie HttpOnly — nunca exposto ao JS do cliente
- Fluxo de recuperação de senha por código enviado via e-mail (Resend)

## Endpoints

| Recurso | Rota base | Principais ações |
|---|---|---|
| Auth | `/api/auth` | `register`, `login`, `google`, `refresh`, `logout`, `forgot-password`, `verify-reset-code`, `reset-password` |
| Receitas | `/api/recipe` | `get-all-recipes`, `get-recipe-by-id`, `create`, `update`, `delete` |
| Categorias | `/api/categories` | `get-all` |
| Usuário | `/api/user` | `me` (GET/PUT) |
| Chat da receita | `/api/recipes/{recipeId}/messages` | GET, POST, DELETE |

Documentação completa e testável em `/swagger` com a API rodando localmente.

## Deploy

A API roda em container Docker (ver `Dockerfile` na raiz), publicada no **Render**. O CORS está configurado para aceitar requisições apenas do domínio do frontend (Vercel), com `AllowCredentials` habilitado — necessário para o cookie de refresh token funcionar entre domínios diferentes.

## TODO

Funcionalidades e melhorias planejadas para o projeto (compartilhadas com o roadmap do frontend):

- [ ] 2FA
- [ ] Busca de receitas
- [ ] Compartilhar receita via PDF
- [ ] Compartilhar receita via app
- [ ] Chat geral (não vinculado a uma receita específica)
- [ ] **RAG** — busca semântica sobre as receitas (provável uso de `pgvector`)
- [ ] **MCP** — servidor MCP expondo:
  - [ ] tool para criar receita
  - [ ] tool para buscar receita por ingrediente

## Decisões de arquitetura

- **Clean Architecture** foi escolhida para manter as regras de negócio isoladas de detalhes de infraestrutura (banco, providers externos), facilitando testes e eventual troca de tecnologia em qualquer camada externa.
- **JWT + Refresh Token via cookie HttpOnly**: o access token curto reduz a janela de exposição em caso de vazamento; o refresh token fica inacessível a scripts no browser, mitigando XSS.
- **User Secrets** em vez de segredos no `appsettings.json`, evitando credenciais versionadas no repositório.
