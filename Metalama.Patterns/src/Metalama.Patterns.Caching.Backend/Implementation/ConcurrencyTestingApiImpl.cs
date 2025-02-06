// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// Ported from PostSharp.Patterns.Common/Threading

namespace Metalama.Patterns.Caching.Implementation;

internal abstract class ConcurrencyTestingApiImpl
{
    public abstract void TraceEvent( string message );
}