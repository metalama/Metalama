// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Backends;

[PublicAPI]
public record CachingBackendConfiguration
{
    public bool IsBehindL1 { get; internal init; }

    public string? DebugName { get; set; }
}