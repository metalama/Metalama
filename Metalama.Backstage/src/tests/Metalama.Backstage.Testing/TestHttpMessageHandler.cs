// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly TestHttpClientFactory _parent;

    public TestHttpMessageHandler( TestHttpClientFactory parent )
    {
        this._parent = parent;
    }

    protected sealed override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
    {
        var response = await this.SendCoreAsync( request, cancellationToken );

        this._parent.ProcessedRequests.Add( (request, response) );

        return response;
    }

    // ReSharper disable once UnusedParameter.Global
    protected virtual Task<HttpResponseMessage> SendCoreAsync( HttpRequestMessage request, CancellationToken cancellationToken )
        => Task.FromResult( new HttpResponseMessage( HttpStatusCode.OK ) );
}