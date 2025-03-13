// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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