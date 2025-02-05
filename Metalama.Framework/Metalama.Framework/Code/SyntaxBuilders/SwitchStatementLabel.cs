// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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