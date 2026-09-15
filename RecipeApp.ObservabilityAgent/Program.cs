using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RecipeApp.ObservabilityAgent.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ILogAnalyticsService, LogAnalyticsService>();
        services.AddHttpClient<IGeminiAnalysisService, GeminiAnalysisService>();
        services.AddSingleton<ObservabilityAgentService>();
    })
    .Build();

host.Run();