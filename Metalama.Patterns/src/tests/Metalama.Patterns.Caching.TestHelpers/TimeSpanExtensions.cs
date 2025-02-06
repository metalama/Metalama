// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.TestHelpers;

[PublicAPI]
public static class TimeSpanExtensions
{
    public static TimeSpan Multiply( this TimeSpan t, double m )
    {
        return TimeSpan.FromMilliseconds( t.TotalMilliseconds * m );
    }
}