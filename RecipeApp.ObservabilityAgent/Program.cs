using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RecipeApp.ObservabilityAgent.Services;
using Resend;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<ILogAnalyticsService, LogAnalyticsService>();
        services.AddHttpClient<IGeminiAnalysisService, GeminiAnalysisService>();

        services.AddResend(o =>
        {
            o.ApiToken = context.Configuration["Resend:ApiKey"]!;
        });
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        services.AddScoped<ObservabilityAgentService>();
    })
    .Build();

host.Run();