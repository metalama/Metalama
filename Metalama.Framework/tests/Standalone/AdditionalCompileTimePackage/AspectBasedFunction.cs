using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctionTriggerAspect
{
    public class AspectBasedFunction
    {
        private readonly ILogger _logger;

        public AspectBasedFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AspectBasedFunction>();
        }

        [Function("AspectHelloWorld")]
        public HttpResponseData Run([TriggerAspect] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions Aspect Based Function!");

            return response;
        }
    }
}
