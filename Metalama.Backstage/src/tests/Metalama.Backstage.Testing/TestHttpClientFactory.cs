// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing
{
    public sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly ILogger _logger;

        public TestHttpClientFactory( IServiceProvider serviceProvider )
        {
            this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(TestHttpClientFactory) );
        }

        private readonly List<(Predicate<HttpRequestMessage> Filter, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Hook)>
            _hooks = new();

        public ConcurrentBag<(HttpRequestMessage Request, HttpResponseMessage Response)> ProcessedRequests { get; private set; } = [];

        public void InsertHook( Predicate<HttpRequestMessage> filter, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> hook )
        {
            this._hooks.Insert( 0, (filter, hook) );
        }

        public void ClearProcessedRequests() => this.ProcessedRequests = [];

        public void ClearHooks() => this._hooks.Clear();

        public HttpClient Create() => new( new Handler( this ) );

        private sealed class Handler : HttpMessageHandler
        {
            private readonly TestHttpClientFactory _parent;

            public Handler( TestHttpClientFactory parent )
            {
                this._parent = parent;
            }

            protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
            {
                var hook = this._parent._hooks.FirstOrDefault( h => h.Filter( request ) )
                    .Hook;

                HttpResponseMessage response;

                if ( hook != null )
                {
                    this._parent._logger.Trace?.Log( $"Hooking '{request.RequestUri}'." );
                    response = await hook( request, cancellationToken );
                }
                else
                {
                    this._parent._logger.Trace?.Log( $"No hook was registered for '{request.RequestUri}'. Returning dummy response." );

                    response = new HttpResponseMessage( HttpStatusCode.OK )
                    {
                        Content = new StringContent( $"<error>No hook was registered for '{request.RequestUri}'.</error>" )
                    };
                }

                this._parent.ProcessedRequests.Add( (request, response) );

                return response;
            }
        }
    }
}