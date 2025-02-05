// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Testing;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#if NET
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.IO;
using System.Linq;
#endif

namespace Metalama.Backstage.Tests.Telemetry;

internal class TelemetryTestsPutMessageHandler : TestHttpMessageHandler
{
#if NET
    private readonly TelemetryPutHandler _telemetryPutHandler;
#endif

    // ReSharper disable UnusedParameter.Local
    public TelemetryTestsPutMessageHandler( IServiceProvider serviceProvider, string outputDirectory, TestHttpClientFactory factory ) : base( factory )
    {
        // ReSharper restore UnusedParameter.Local
#if NET
        this._telemetryPutHandler = new TelemetryPutHandler( serviceProvider, outputDirectory );
#endif
    }

#pragma warning disable CS1998
    protected override async Task<HttpResponseMessage> SendCoreAsync( HttpRequestMessage requestMessage, CancellationToken cancellationToken )
#pragma warning restore CS1998
    {
        HttpResponseMessage responseMessage;

#if NET
        var request = await MessageToRequest( requestMessage );
        var response = await this._telemetryPutHandler.HandleAsync( request, default );

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext() { RequestServices = serviceProvider };

        await response.ExecuteAsync( httpContext );
        responseMessage = new HttpResponseMessage( (HttpStatusCode) httpContext.Response.StatusCode );
#else
        responseMessage = new HttpResponseMessage( HttpStatusCode.OK );
#endif

        return responseMessage;
    }

#if NET
    private static async Task<HttpRequest> MessageToRequest( HttpRequestMessage requestMessage )
    {
        // https://stackoverflow.com/a/68453301/4100001
        if ( requestMessage.RequestUri == null )
        {
            throw new ArgumentException( $"{nameof(requestMessage)}.{nameof(requestMessage.RequestUri)} is null.", nameof(requestMessage) );
        }

        if ( requestMessage.Content == null )
        {
            throw new ArgumentException( $"{nameof(requestMessage)}.{nameof(requestMessage.Content)} is null.", nameof(requestMessage) );
        }

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = requestMessage.Method.Method;
        httpContext.Request.Scheme = requestMessage.RequestUri.Scheme;
        httpContext.Request.Host = new HostString( requestMessage.RequestUri.Host, requestMessage.RequestUri.Port );
        httpContext.Request.Path = requestMessage.RequestUri.AbsolutePath;
        httpContext.Request.QueryString = new QueryString( requestMessage.RequestUri.Query );

        foreach ( var header in requestMessage.Content.Headers )
        {
            httpContext.Request.Headers.Add( header.Key, new StringValues( header.Value.ToArray() ) );
        }

        var stream = new MemoryStream();
        await requestMessage.Content.CopyToAsync( stream );
        await stream.FlushAsync();
        stream.Position = 0;
        httpContext.Request.Body = stream;

        return httpContext.Request;
    }
#endif
}