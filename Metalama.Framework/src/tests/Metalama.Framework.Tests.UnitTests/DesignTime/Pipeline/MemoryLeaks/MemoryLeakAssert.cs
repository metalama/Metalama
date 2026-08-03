// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Assertions on the reachability of objects that a design-time editing session was expected to release.
/// </summary>
/// <remarks>
/// A failure reported by these assertions includes the chain of references that retains the object, computed by
/// <see cref="RetentionPathFinder"/>. Without that chain, the only information a failure conveys is that something,
/// somewhere, retains a compilation, which is not enough to act upon.
/// </remarks>
internal static class MemoryLeakAssert
{
    /// <summary>
    /// Asserts that the target of a weak reference has been collected, and explains the retention if it has not.
    /// </summary>
    /// <param name="weakReference">A weak reference to the object that was expected to be collected.</param>
    /// <param name="description">A description of the object, used in the failure message.</param>
    /// <param name="roots">The objects that play the role of garbage-collection roots in the failure analysis.</param>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static void Collected( WeakReference weakReference, string description, params (string Name, object Root)[] roots )
    {
        GarbageCollectionHelper.Collect();

        var target = weakReference.Target;

        if ( target == null )
        {
            return;
        }

        Assert.Fail( BuildFailureMessage( target, description, roots ) );
    }

    /// <summary>
    /// Asserts that at most <paramref name="expectedMaximum"/> of the given weak references are still alive.
    /// </summary>
    /// <remarks>
    /// The design-time host legitimately retains the most recent version of a project, therefore an assertion that
    /// nothing at all survives an editing session would be wrong. This overload expresses the correct expectation:
    /// the number of surviving versions must be bounded by a constant, and in particular must not grow with the
    /// number of edits.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static void AtMostAlive(
        IReadOnlyList<WeakReference> weakReferences,
        int expectedMaximum,
        string description,
        params (string Name, object Root)[] roots )
    {
        GarbageCollectionHelper.Collect();

        List<int> aliveIndices = new();

        for ( var i = 0; i < weakReferences.Count; i++ )
        {
            if ( weakReferences[i].IsAlive )
            {
                aliveIndices.Add( i );
            }
        }

        if ( aliveIndices.Count <= expectedMaximum )
        {
            return;
        }

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(
            $"{aliveIndices.Count} of {weakReferences.Count} {description} are still alive, but at most {expectedMaximum} were expected." );

        stringBuilder.AppendLine( $"Alive indices: {string.Join( ", ", aliveIndices )}." );

        // Explain the retention of the oldest survivor, which is the most informative one: the most recent versions
        // are expected to be alive, whereas the oldest one should have been released first.
        var oldestSurvivor = weakReferences[aliveIndices[0]].Target;

        if ( oldestSurvivor != null )
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine( BuildFailureMessage( oldestSurvivor, $"The oldest survivor (index {aliveIndices[0]})", roots ) );
        }

        Assert.Fail( stringBuilder.ToString() );
    }

    /// <summary>
    /// Builds a failure message that includes the retention chain of <paramref name="target"/>, when one can be found.
    /// </summary>
    private static string BuildFailureMessage( object target, string description, (string Name, object Root)[] roots )
    {
        var finder = new RetentionPathFinder();

        if ( finder.TryFindPath( target, out var path, roots ) )
        {
            return $"{description} is still reachable. Retention path:{Environment.NewLine}{path}";
        }

        var reason = finder.IsExhausted
            ? "the search was exhausted before completing, therefore the absence of a path is not conclusive"
            : "no path exists from the given roots, therefore the object is retained by a root that was not supplied, "
              + "such as a static field, a running task, or a local variable of the test itself";

        return $"{description} is still reachable, but no retention path was found: {reason}. "
               + $"The search visited {finder.VisitedObjectCount} objects.";
    }
}
