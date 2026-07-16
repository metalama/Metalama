// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Provides synchronization points that let a test deterministically control the interleaving of concurrent code.
/// This service is optional and is never registered in production: when it is absent, synchronization points are
/// skipped entirely.
/// </summary>
/// <remarks>
/// <para>
/// Resolve this from the <see cref="IServiceProvider"/> passed to the component under test (e.g.
/// <see cref="AwaitableEvent"/>). Because it is an ordinary service rather than global mutable state, several
/// tests can drive their own synchronization points concurrently without interfering with each other.
/// </para>
/// <para>
/// Callers reach synchronization points through <c>[Conditional("DEBUG")]</c> helpers, so in a Release build the
/// calls - and therefore the cost of this abstraction - are compiled away entirely.
/// </para>
/// </remarks>
internal interface ITestSynchronizationProvider
{
    /// <summary>
    /// Called by the code under test at a synchronization point. An implementation typically blocks until the test
    /// releases the point, and returns immediately for points the test is not interested in.
    /// </summary>
    /// <param name="name">The name of the synchronization point. Names are not required to be unique: a test arms
    /// the name it cares about, and the point trips at whichever matching site the exercised code path reaches.</param>
    void SyncPoint( string name );
}
