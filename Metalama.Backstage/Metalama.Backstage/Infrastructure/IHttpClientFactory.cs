// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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