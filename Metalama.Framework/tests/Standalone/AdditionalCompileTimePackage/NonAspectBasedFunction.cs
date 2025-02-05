using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctionTriggerAspect
{
    public class NonAspectBasedFunction
    {
        private readonly ILogger _logger;

        public NonAspectBasedFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AspectBasedFunction>();
        }

        [Function("NonAspectHelloWorld")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "NonAspectBasedFunction/HelloWorld")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions Non-aspect Based Function!");

            return response;
        }
    }
}
