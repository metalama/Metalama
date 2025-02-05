// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace Metalama.Backstage.Testing
{
    public class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<TestHttpClientFactory, TestHttpMessageHandler> _createHandler;

        public ConcurrentBag<(HttpRequestMessage Request, HttpResponseMessage Response)> ProcessedRequests { get; private set; } = [];

        public void Reset() => this.ProcessedRequests = [];

        public TestHttpClientFactory( Func<TestHttpClientFactory, TestHttpMessageHandler>? createHandler = null )
        {
            this._createHandler = createHandler ?? (x => new TestHttpMessageHandler( x ));
        }

        public HttpClient Create() => new( this._createHandler( this ) );
    }
}