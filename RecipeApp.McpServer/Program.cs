using Microsoft.EntityFrameworkCore;
using RecipeApp.Infrastructure.Persistence;
using RecipeApp.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseVector()));

builder.Services.AddHttpClient();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<RecipeTools>();

var app = builder.Build();

app.MapMcp();

app.Run();