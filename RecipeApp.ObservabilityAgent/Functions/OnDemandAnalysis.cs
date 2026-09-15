using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RecipeApp.ObservabilityAgent.Services;
using System.Net;

namespace RecipeApp.ObservabilityAgent.Functions;

public class OnDemandAnalysis
{
    private readonly ObservabilityAgentService _agent;
    public OnDemandAnalysis(ObservabilityAgentService agent) => _agent = agent;

    [Function("OnDemandAnalysis")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        var result = await _agent.RunAnalysisAsync(TimeSpan.FromHours(1));
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(result);
        return response;
    }
}