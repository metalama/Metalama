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
    /// Gets the indices of the source arguments that the new call does not pass and that are evaluated before the value of this argument. The
    /// argument is written <c>CallSiteHelper.DropBefore( D, value )</c>, where <c>D</c> is the dropped value, or a tuple of the dropped values when
    /// there are several.
    /// </summary>
    public ImmutableArray<int> PrecedingDiscards { get; init; }

    /// <summary>
    /// Gets the indices of the source arguments that the new call does not pass and that are evaluated after the value of this argument. The
    /// argument is written <c>CallSiteHelper.DropAfter( value, D )</c>.
    /// </summary>
    public ImmutableArray<int> FollowingDiscards { get; init; }

    /// <summary>
    /// Gets the type syntax of <c>Metalama.Framework.RunTime.CallSiteHelper</c>, or <c>null</c> when the argument has no dropped value.
    /// </summary>
    public TypeSyntax? DropHelperType { get; init; }

    /// <summary>
    /// Gets the type argument of the kept value of the calls of <c>CallSiteHelper</c>, or <c>null</c> when the type arguments are inferred. When it is
    /// not <c>null</c>, both type arguments are written, so that the kept value is converted to the type of the parameter as in the original call.
    /// </summary>
    public TypeSyntax? KeepTypeArgument { get; init; }

    /// <summary>
    /// Gets the type argument of the values of <see cref="PrecedingDiscards"/> when <see cref="KeepTypeArgument"/> is not <c>null</c>.
    /// </summary>
    public TypeSyntax? PrecedingDropTypeArgument { get; init; }

    /// <summary>
    /// Gets the type argument of the values of <see cref="FollowingDiscards"/> when <see cref="KeepTypeArgument"/> is not <c>null</c>.
    /// </summary>
    public TypeSyntax? FollowingDropTypeArgument { get; init; }

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
