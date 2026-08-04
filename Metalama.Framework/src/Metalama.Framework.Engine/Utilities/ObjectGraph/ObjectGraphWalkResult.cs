// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities.ObjectGraph;

/// <summary>
/// The outcome of a walk performed by <see cref="ObjectGraphWalker.Walk"/>.
/// </summary>
/// <param name="VisitedObjectCount">The number of distinct objects visited.</param>
/// <param name="IsExhausted">
/// A value indicating whether the walk ended because it reached <see cref="ObjectGraphWalkerOptions.MaxObjects"/> or
/// <see cref="ObjectGraphWalkerOptions.Timeout"/>. When this is <c>true</c>, the graph was not explored completely and
/// the absence of a result proves nothing.
/// </param>
/// <param name="IsStopped">
/// A value indicating whether the walk ended because the visitor returned <see cref="ObjectGraphAction.Stop"/>.
/// </param>
internal readonly record struct ObjectGraphWalkResult( int VisitedObjectCount, bool IsExhausted, bool IsStopped )
{
    /// <summary>
    /// Gets a value indicating whether the whole graph reachable from the roots was explored, that is, the walk was
    /// neither exhausted nor stopped by the visitor.
    /// </summary>
    public bool IsComplete => !this.IsExhausted && !this.IsStopped;
}
