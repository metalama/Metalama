// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics;

// Ported from PostSharp.Patterns.Common/Threading

namespace Metalama.Patterns.Caching.Implementation;

internal static class ConcurrencyTestingApi
{
    // Field will be set by test harness.
    // ReSharper disable once MemberCanBePrivate.Global
#pragma warning disable CS0649
#pragma warning disable SA1401
    public static ConcurrencyTestingApiImpl? Implementation;
#pragma warning restore SA1401
#pragma warning restore CS0649

    [Conditional( "DEBUG" )]
    public static void TraceEvent( string message ) => Implementation?.TraceEvent( message );
}