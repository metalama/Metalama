// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// Options of the <see cref="CacheKeyBuilder"/> class.
/// </summary>
[PublicAPI]
public record CacheKeyBuilderOptions
{
    public int MaxKeySize { get; init; } = 1024;
}