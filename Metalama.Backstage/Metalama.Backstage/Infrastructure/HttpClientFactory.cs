// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Net.Http;

namespace Metalama.Backstage.Infrastructure;

internal class HttpClientFactory : IHttpClientFactory
{
    public HttpClient Create() => new();
}