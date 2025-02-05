// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

[CompileTime]
public static class OperatorKindExtensions
{
    public static OperatorCategory GetCategory( this OperatorKind operatorKind )
        => operatorKind switch
        {
            OperatorKind.None => OperatorCategory.None,
            OperatorKind.ImplicitConversion => OperatorCategory.Conversion,
            OperatorKind.ExplicitConversion => OperatorCategory.Conversion,
            OperatorKind.Addition => OperatorCategory.Binary,
            OperatorKind.BitwiseAnd => OperatorCategory.Binary,
            OperatorKind.BitwiseOr => OperatorCategory.Binary,
            OperatorKind.Decrement => OperatorCategory.Unary,
            OperatorKind.Division => OperatorCategory.Binary,
            OperatorKind.Equality => OperatorCategory.Binary,
            OperatorKind.ExclusiveOr => OperatorCategory.Binary,
            OperatorKind.False => OperatorCategory.Unary,
            OperatorKind.GreaterThan => OperatorCategory.Binary,
            OperatorKind.GreaterThanOrEqual => OperatorCategory.Binary,
            OperatorKind.Increment => OperatorCategory.Unary,
            OperatorKind.Inequality => OperatorCategory.Binary,
            OperatorKind.LeftShift => OperatorCategory.Binary,
            OperatorKind.LessThan => OperatorCategory.Binary,
            OperatorKind.LessThanOrEqual => OperatorCategory.Binary,
            OperatorKind.LogicalNot => OperatorCategory.Unary,
            OperatorKind.Modulus => OperatorCategory.Binary,
            OperatorKind.Multiply => OperatorCategory.Binary,
            OperatorKind.OnesComplement => OperatorCategory.Unary,
            OperatorKind.RightShift => OperatorCategory.Binary,
            OperatorKind.Subtraction => OperatorCategory.Binary,
            OperatorKind.True => OperatorCategory.Unary,
            OperatorKind.UnaryNegation => OperatorCategory.Unary,
            OperatorKind.UnaryPlus => OperatorCategory.Unary,
            _ => throw new ArgumentOutOfRangeException( nameof(operatorKind), operatorKind, null )
        };
}