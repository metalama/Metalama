// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
