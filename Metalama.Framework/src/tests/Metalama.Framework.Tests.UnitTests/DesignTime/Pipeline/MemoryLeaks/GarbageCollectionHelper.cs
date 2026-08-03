// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Forces the garbage collector to run to completion, so that a test can assert that an object is no longer reachable.
/// </summary>
/// <remarks>
/// <para>
/// A single call to <see cref="GC.Collect()"/> is not sufficient for this kind of assertion. An object that has a
/// finalizer is promoted to the finalization queue by the first collection and is only reclaimed by a subsequent one,
/// therefore at least two rounds of collection and finalization are required. Roslyn objects reachable from a
/// <see cref="ConditionalWeakTable{TKey,TValue}"/> add a second reason: the entries of such a table are resolved
/// during the mark phase, and a value that has itself become unreachable may only be observed as such once the key
/// has been collected in an earlier round.
/// </para>
/// <para>
/// The collection is requested as blocking and compacting on the maximum generation, because the default
/// non-blocking background collection may return before the large object heap, where syntax trees and compilations
/// typically reside, has been swept.
/// </para>
/// </remarks>
internal static class GarbageCollectionHelper
{
    /// <summary>
    /// The number of collection rounds performed by <see cref="Collect"/>.
    /// </summary>
    private const int _collectionRounds = 4;

    /// <summary>
    /// Performs several rounds of blocking, compacting garbage collection, waiting for finalizers between rounds.
    /// </summary>
    public static void Collect()
    {
        for ( var i = 0; i < _collectionRounds; i++ )
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );
            GC.WaitForPendingFinalizers();
        }

        GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );
    }

    /// <summary>
    /// Returns the number of live objects among the given weak references, after forcing a full collection.
    /// </summary>
    /// <remarks>
    /// The method is marked as not inlinable so that the caller cannot keep a temporary of the enumeration alive on
    /// its stack frame, which would defeat the purpose of the measurement.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static int CountAlive( params WeakReference[] weakReferences )
    {
        Collect();

        var count = 0;

        foreach ( var weakReference in weakReferences )
        {
            if ( weakReference.IsAlive )
            {
                count++;
            }
        }

        return count;
    }
}
