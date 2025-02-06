// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// The <see cref="FormattingRole"/> for <c>Metalama.Patterns.Caching</c>.
/// </summary>
[PublicAPI]
public sealed class CacheKeyFormatting : FormattingRole
{
    public static CacheKeyFormatting Instance { get; } = new();

    private CacheKeyFormatting() : base( "Caching" ) { }
}