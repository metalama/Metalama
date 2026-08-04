// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.ObjectGraph;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;
using System.Threading;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Finds the shortest chain of object references from a set of roots to a target object.
/// </summary>
/// <remarks>
/// <para>
/// A test that asserts that a <see cref="Compilation"/> has been collected reports a failure that is difficult to act
/// upon: the assertion says that something retains the compilation, but not what. This class answers that question.
/// The roots given to <see cref="TryFindPath"/> are the objects that a design-time host keeps alive for the lifetime
/// of a project, typically the pipeline factory and the services it owns, and the target is the object that the test
/// expected to be collected.
/// </para>
/// <para>
/// The traversal itself, including the handling of weak references and of the ephemeron semantics of a
/// <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey,TValue}"/>, belongs to
/// <see cref="ObjectGraphWalker"/>, which the product code shares. This class only supplies the stop rule and formats
/// the result.
/// </para>
/// </remarks>
internal sealed class RetentionPathFinder
{
    /// <summary>
    /// The maximum number of distinct objects visited before the search gives up.
    /// </summary>
    private const int _defaultMaxObjects = 400_000;

    private readonly ObjectGraphWalker _walker;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionPathFinder"/> class.
    /// </summary>
    /// <param name="maxObjects">The maximum number of distinct objects to visit. The search reports failure when the budget is exhausted.</param>
    /// <param name="timeout">The maximum duration of the search. A <c>null</c> value means one minute.</param>
    public RetentionPathFinder( int maxObjects = _defaultMaxObjects, TimeSpan? timeout = null )
    {
        var options = ObjectGraphWalkerOptions.Default with { MaxObjects = maxObjects };

        if ( timeout != null )
        {
            options = options with { Timeout = timeout.Value };
        }

        this._walker = new ObjectGraphWalker( options );
    }

    /// <summary>
    /// Gets the number of distinct objects visited by the last call to <see cref="TryFindPath"/>.
    /// </summary>
    public int VisitedObjectCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the last call to <see cref="TryFindPath"/> stopped because it exhausted its
    /// budget of objects or its timeout, in which case a negative result is inconclusive.
    /// </summary>
    public bool IsExhausted { get; private set; }

    /// <summary>
    /// Attempts to find the shortest chain of strong or conditional references from one of <paramref name="roots"/>
    /// to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The object whose retention must be explained.</param>
    /// <param name="path">On success, a human-readable description of the chain, one hop per line.</param>
    /// <param name="roots">The objects from which the search starts, each paired with the name to display for it.</param>
    /// <returns><c>true</c> if a chain was found.</returns>
    public bool TryFindPath( object target, out string? path, params (string Name, object Root)[] roots )
    {
        string? foundPath = null;

        var result = this._walker.Walk(
            roots,
            node =>
            {
                if ( ReferenceEquals( node.Object, target ) )
                {
                    foundPath = node.Parent == null
                        ? $"{node.Label} ({ObjectGraphNode.FormatType( node.Object.GetType() )}) is the target itself."
                        : node.FormatPath();

                    return ObjectGraphAction.Stop;
                }

                // A root is always traversed, because the caller chose it deliberately.
                return node.Parent == null || ShouldTraverse( node.Object ) ? ObjectGraphAction.Traverse : ObjectGraphAction.Skip;
            } );

        this.VisitedObjectCount = result.VisitedObjectCount;
        this.IsExhausted = result.IsExhausted;
        path = foundPath;

        return foundPath != null;
    }

    /// <summary>
    /// Determines whether the object graph should be explored through the given object.
    /// </summary>
    /// <remarks>
    /// Objects that are themselves plausible targets of a retention assertion, and objects belonging to the reflection
    /// or threading infrastructure, are not traversed. Traversing them would multiply the cost of the search without
    /// producing a chain that this codebase could act upon.
    /// </remarks>
    private static bool ShouldTraverse( object obj )
    {
        switch ( obj )
        {
            case Compilation:
            case SyntaxTree:
            case SemanticModel:
            case ISymbol:
            case string:
            case Type:
            case Assembly:
            case Module:
            case MemberInfo:
            case ParameterInfo:
            case Thread:
            case AppDomain:
                return false;

            default:
                return true;
        }
    }
}
