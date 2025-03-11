// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Options for the <see cref="CacheSynchronizer"/> class.
/// </summary>
[PublicAPI]
public record CacheSynchronizerConfiguration
{
    /// <summary>
    /// Gets the prefix of messages sent by the <see cref="CacheSynchronizer"/>.
    /// Messages received by the <see cref="CacheSynchronizer.OnMessageReceived"/> method are
    /// ignored if they don't start with the proper prefix.
    /// </summary>
    public string Prefix { get; init; } = DefaultPrefix;

    public const string DefaultPrefix = "invalidate";
}