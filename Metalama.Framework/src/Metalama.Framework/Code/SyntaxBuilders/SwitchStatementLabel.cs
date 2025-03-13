// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Represents the label of a <c>switch case</c> (i.e. the literal or tuple literal to which the expression is compared).
/// Only single literals or tuple of literals can be represented. 
/// </summary>
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
    public static SwitchStatementLabel CreateLiteral( params object[] values ) => new( values.Select( TypedConstant.Create ).ToList() );

    /// <summary>
    /// Creates a literal <see cref="SwitchStatementLabel"/> by giving the literals as <see cref="TypedConstant"/> values.
    /// </summary>
    public static SwitchStatementLabel CreateLiteral( params TypedConstant[] values ) => new( values );

    /// <summary>
    /// Creates a literal <see cref="SwitchStatementLabel"/> by giving the literals as <see cref="TypedConstant"/> values.
    /// </summary>
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