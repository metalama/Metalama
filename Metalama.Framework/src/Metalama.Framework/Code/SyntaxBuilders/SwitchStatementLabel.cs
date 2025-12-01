// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Represents the label of a <c>switch case</c> (i.e., the literal or tuple literal to which the expression is compared).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SwitchStatementLabel"/> represents the case label in a switch statement's case section. It supports single literals
/// (e.g., <c>case 1:</c>) and tuple literals (e.g., <c>case (1, "a"):</c>).
/// </para>
/// <para>
/// Use the static <see cref="CreateLiteral(object[])"/> method to create labels from intrinsic values like <see cref="int"/>,
/// <see cref="string"/>, or enum values. For more control over types, use <see cref="CreateLiteral(TypedConstant[])"/>
/// with explicitly typed constants.
/// </para>
/// <para>
/// <b>Limitation:</b> Only constant literals and tuple literals are supported. Advanced C# pattern matching syntax
/// (such as type patterns, property patterns, relational patterns, or logical patterns) is not supported.
/// For complex pattern matching, consider using <see cref="StatementBuilder"/> to construct the switch statement as text.
/// </para>
/// </remarks>
/// <seealso cref="SwitchStatementBuilder"/>
/// <seealso cref="TypedConstant"/>
/// <seealso href="@run-time-statements"/>
/// <seealso href="@templates"/>
[CompileTime]
public sealed class SwitchStatementLabel
{
    /// <summary>
    /// Gets the list of literals in the tuple.
    /// </summary>
    public IReadOnlyList<TypedConstant> Values { get; }

    /// <summary>
    /// Creates a literal <see cref="SwitchStatementLabel"/> by giving the literals as intrinsic values (<see cref="string"/>, <see cref="int"/>, ...).
    /// </summary>
    /// <param name="values">The literal values to match.</param>
    /// <returns>A new <see cref="SwitchStatementLabel"/>.</returns>
    public static SwitchStatementLabel CreateLiteral( params object[] values ) => new( values.Select( TypedConstant.Create ).ToList() );

    /// <summary>
    /// Creates a literal <see cref="SwitchStatementLabel"/> by giving the literals as <see cref="TypedConstant"/> values.
    /// </summary>
    /// <param name="values">The literal values to match.</param>
    /// <returns>A new <see cref="SwitchStatementLabel"/>.</returns>
    public static SwitchStatementLabel CreateLiteral( params TypedConstant[] values ) => new( values );

    /// <summary>
    /// Creates a literal <see cref="SwitchStatementLabel"/> by giving the literals as <see cref="TypedConstant"/> values.
    /// </summary>
    /// <param name="values">The literal values to match.</param>
    /// <returns>A new <see cref="SwitchStatementLabel"/>.</returns>
    public static SwitchStatementLabel CreateLiteral( IReadOnlyList<TypedConstant> values ) => new( values );

    private SwitchStatementLabel( IReadOnlyList<TypedConstant> values )
    {
        if ( values.Count == 0 )
        {
            throw new ArgumentOutOfRangeException( nameof(values), "At least one value is required." );
        }

        this.Values = values;
    }
}