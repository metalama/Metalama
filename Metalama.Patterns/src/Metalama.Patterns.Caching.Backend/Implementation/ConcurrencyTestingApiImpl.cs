// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Ported from PostSharp.Patterns.Common/Threading

namespace Metalama.Patterns.Caching.Implementation;

internal abstract class ConcurrencyTestingApiImpl
{
    public abstract void TraceEvent( string message );
}