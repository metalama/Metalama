// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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