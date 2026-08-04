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
    /// Asserts that the target of a weak reference is still alive and that what retains it is the expected route.
    /// </summary>
    /// <param name="weakReference">A weak reference to the object that is known to be retained.</param>
    /// <param name="expectedInPath">
    /// A fragment of the retention chain, usually the name of the type or of the field that carries the reference.
    /// </param>
    /// <param name="description">A description of the object, used in the failure messages.</param>
    /// <param name="roots">The objects that play the role of garbage-collection roots in the analysis.</param>
    /// <remarks>
    /// <para>
    /// This is the inverse of <see cref="Collected"/> and is used to record a retention that is known and tracked but
    /// not yet fixed. Asserting only that the object is alive would be a weak record, because it would hold whether the
    /// documented route retains the object or something else does, and it would go on holding after the documented
    /// route was removed. Naming the route makes the assertion say what it means, and makes it fail in both of the
    /// directions that matter: when the retention is fixed, and when the retention survives for a different reason
    /// than the one written down.
    /// </para>
    /// <para>
    /// A failure of the first kind is the signal to replace the call with <see cref="Collected"/>.
    /// </para>
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static void RetainedThrough(
        WeakReference weakReference,
        string expectedInPath,
        string description,
        params (string Name, object Root)[] roots )
    {
        GarbageCollectionHelper.Collect();

        var target = weakReference.Target;

        Assert.True(
            target != null,
            $"{description} was released, so the retention through {expectedInPath} is gone. If the issue that tracks "
            + $"it has been fixed, replace this control with {nameof(Collected)}." );

        var finder = new RetentionPathFinder();

        if ( !finder.TryFindPath( target!, out var path, roots ) )
        {
            Assert.Fail(
                $"{description} is retained, as expected, but no retention path was found from the given roots, "
                + $"therefore this control cannot confirm that {expectedInPath} is what retains it. The search visited "
                + $"{finder.VisitedObjectCount} objects and "
                + (finder.IsExhausted ? "was exhausted before completing." : "completed.") );
        }

        Assert.True(
            path!.Contains( expectedInPath, StringComparison.Ordinal ),
            $"{description} is retained, but not through {expectedInPath}. Retention path:{Environment.NewLine}{path}" );
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
