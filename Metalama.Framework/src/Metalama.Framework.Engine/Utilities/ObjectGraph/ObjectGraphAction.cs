// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities.ObjectGraph;

/// <summary>
/// The decision that the visitor of an <see cref="ObjectGraphWalker"/> takes for a newly discovered object.
/// </summary>
internal enum ObjectGraphAction
{
    /// <summary>
    /// Follows the references of the object.
    /// </summary>
    Traverse,

    /// <summary>
    /// Does not follow the references of the object, but continues the walk elsewhere.
    /// </summary>
    /// <remarks>
    /// This is how a caller expresses that an object is a boundary of the region it is responsible for, such as a
    /// <see cref="Microsoft.CodeAnalysis.Compilation"/>, whose internal graph is large and belongs to another
    /// component.
    /// </remarks>
    Skip,

    /// <summary>
    /// Ends the walk immediately.
    /// </summary>
    /// <remarks>
    /// A caller that searches for a single object uses this as soon as it has found it.
    /// </remarks>
    Stop
}
