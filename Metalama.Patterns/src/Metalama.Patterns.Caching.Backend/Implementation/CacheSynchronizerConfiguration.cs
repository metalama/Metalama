// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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