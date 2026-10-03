// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// One element of the argument list of a redirected invocation, in the order in which it is written.
/// </summary>
/// <param name="Kind">The kind of the argument.</param>
/// <param name="SourceArgumentIndex">For <see cref="RedirectedArgumentKind.SourceArgument"/>, the index of the argument in the argument list of the
/// source invocation.</param>
/// <param name="Value">For <see cref="RedirectedArgumentKind.Value"/>, the expression of the argument.</param>
/// <param name="Name">The parameter name with which the argument is written.</param>
internal readonly record struct CallSiteArgumentPlanItem( RedirectedArgumentKind Kind, int SourceArgumentIndex, ExpressionSyntax? Value, string Name )
{
    /// <summary>
    /// Gets the indices of the source arguments that the new call does not pass and that are evaluated, and discarded, before the value of this
    /// argument. The argument is written <c>D switch { _ =&gt; value }</c>.
    /// </summary>
    public ImmutableArray<int> PrecedingDiscards { get; init; }

    /// <summary>
    /// Gets the indices of the source arguments that the new call does not pass and that are evaluated, and discarded, after the value of this
    /// argument. The argument is written <c>value switch { var t =&gt; D switch { _ =&gt; t } }</c>, where <c>t</c> is
    /// <see cref="ValueVariableName"/>.
    /// </summary>
    public ImmutableArray<int> FollowingDiscards { get; init; }

    /// <summary>
    /// Gets the name of the pattern variable that holds the value of this argument while <see cref="FollowingDiscards"/> are evaluated.
    /// </summary>
    public string? ValueVariableName { get; init; }

    /// <summary>
    /// Gets the type to which the value of a <see cref="RedirectedArgumentKind.SourceArgument"/> is cast, or <c>null</c>. The argument is written
    /// <c>(T)(value)</c>.
    /// </summary>
    public TypeSyntax? CastType { get; init; }

    /// <summary>
    /// Gets the indices of the source arguments that are the elements of an expanded <c>params</c> argument, which this argument packs into one
    /// collection, or a default array when the argument is not packed. <see cref="SourceArgumentIndex"/> is then -1.
    /// </summary>
    public ImmutableArray<int> PackedElements { get; init; }

    /// <summary>
    /// Gets the element type of the array that packs <see cref="PackedElements"/>, or <c>null</c> to pack them into a collection expression.
    /// </summary>
    public TypeSyntax? PackedArrayElementType { get; init; }

    /// <summary>
    /// Gets the expression that is written when <see cref="PackedElements"/> is empty.
    /// </summary>
    public ExpressionSyntax? PackedEmptyValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether this argument packs the elements of an expanded <c>params</c> argument.
    /// </summary>
    public bool IsPacked => !this.PackedElements.IsDefault;
}
