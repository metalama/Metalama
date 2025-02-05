// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System.Net.Http;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Creates instances of <see cref="HttpClient"/> class.
/// </summary>
[PublicAPI]
public interface IHttpClientFactory : IBackstageService
{
    /// <summary>
    /// Creates a new instance of <see cref="HttpClient"/>.
    /// </summary>
    /// <returns>The new object of <see cref="HttpClient"/>.</returns>
    HttpClient Create();
}