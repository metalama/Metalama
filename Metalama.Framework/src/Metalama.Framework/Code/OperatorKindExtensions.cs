// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Defines extension methods for the <see cref="OperatorKind"/> class.
/// </summary>
[CompileTime]
public static class OperatorKindExtensions
{
    /// <summary>
    /// Gets the <see cref="OperatorCategory"/> for a given <see cref="OperatorKind"/>.
    /// </summary>
    /// <param name="operatorKind">The <see cref="OperatorKind"/> value for which to get the category.</param>
    /// <returns>The <see cref="OperatorCategory"/> corresponding to the specified <see cref="OperatorKind"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="operatorKind"/> is not a valid <see cref="OperatorKind"/> value.</exception>
    public static OperatorCategory GetCategory( this OperatorKind operatorKind )
        => operatorKind switch
        {
            OperatorKind.None => OperatorCategory.None,

            // Conversion operators
            OperatorKind.ImplicitConversion => OperatorCategory.Conversion,
            OperatorKind.ExplicitConversion => OperatorCategory.Conversion,
            OperatorKind.CheckedExplicitConversion => OperatorCategory.Conversion,

            // Binary arithmetic operators
            OperatorKind.Addition => OperatorCategory.Binary,
            OperatorKind.Subtraction => OperatorCategory.Binary,
            OperatorKind.Multiply => OperatorCategory.Binary,
            OperatorKind.Division => OperatorCategory.Binary,
            OperatorKind.Modulus => OperatorCategory.Binary,

            // Checked binary arithmetic operators
            OperatorKind.CheckedAddition => OperatorCategory.Binary,
            OperatorKind.CheckedSubtraction => OperatorCategory.Binary,
            OperatorKind.CheckedMultiply => OperatorCategory.Binary,
            OperatorKind.CheckedDivision => OperatorCategory.Binary,

            // Unary arithmetic operators
            OperatorKind.UnaryNegation => OperatorCategory.Unary,
            OperatorKind.UnaryPlus => OperatorCategory.Unary,
            OperatorKind.Increment => OperatorCategory.Unary,
            OperatorKind.Decrement => OperatorCategory.Unary,

            // Checked unary arithmetic operators
            OperatorKind.CheckedUnaryNegation => OperatorCategory.Unary,
            OperatorKind.CheckedIncrement => OperatorCategory.Unary,
            OperatorKind.CheckedDecrement => OperatorCategory.Unary,

            // Binary bitwise operators
            OperatorKind.BitwiseAnd => OperatorCategory.Binary,
            OperatorKind.BitwiseOr => OperatorCategory.Binary,
            OperatorKind.ExclusiveOr => OperatorCategory.Binary,
            OperatorKind.LeftShift => OperatorCategory.Binary,
            OperatorKind.RightShift => OperatorCategory.Binary,
            OperatorKind.UnsignedRightShift => OperatorCategory.Binary,

            // Unary bitwise operators
            OperatorKind.OnesComplement => OperatorCategory.Unary,

            // Binary comparison operators
            OperatorKind.Equality => OperatorCategory.Binary,
            OperatorKind.Inequality => OperatorCategory.Binary,
            OperatorKind.LessThan => OperatorCategory.Binary,
            OperatorKind.LessThanOrEqual => OperatorCategory.Binary,
            OperatorKind.GreaterThan => OperatorCategory.Binary,
            OperatorKind.GreaterThanOrEqual => OperatorCategory.Binary,

            // Unary logical operators
            OperatorKind.LogicalNot => OperatorCategory.Unary,
            OperatorKind.True => OperatorCategory.Unary,
            OperatorKind.False => OperatorCategory.Unary,

            // Binary logical operators (not user-definable in C#)
            OperatorKind.LogicalAnd => OperatorCategory.Binary,
            OperatorKind.LogicalOr => OperatorCategory.Binary,

            // VB.NET specific binary operators
            OperatorKind.Concatenate => OperatorCategory.Binary,
            OperatorKind.Exponent => OperatorCategory.Binary,
            OperatorKind.IntegerDivision => OperatorCategory.Binary,
            OperatorKind.Like => OperatorCategory.Binary,

            // Compound assignment operators - binary assignment (operand = operand op value)
            OperatorKind.AdditionAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.SubtractionAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.MultiplicationAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.DivisionAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.ModulusAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.BitwiseAndAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.BitwiseOrAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.ExclusiveOrAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.LeftShiftAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.RightShiftAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.UnsignedRightShiftAssignment => OperatorCategory.BinaryAssignment,

            // Unary assignment operators - unary assignment (++operand, --operand)
            OperatorKind.IncrementAssignment => OperatorCategory.UnaryAssignment,
            OperatorKind.DecrementAssignment => OperatorCategory.UnaryAssignment,

            // Checked compound assignment operators - binary assignment
            OperatorKind.CheckedAdditionAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.CheckedSubtractionAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.CheckedMultiplicationAssignment => OperatorCategory.BinaryAssignment,
            OperatorKind.CheckedDivisionAssignment => OperatorCategory.BinaryAssignment,

            // Checked unary assignment operators - unary assignment
            OperatorKind.CheckedIncrementAssignment => OperatorCategory.UnaryAssignment,
            OperatorKind.CheckedDecrementAssignment => OperatorCategory.UnaryAssignment,

            _ => throw new ArgumentOutOfRangeException( nameof(operatorKind), operatorKind, null )
        };
}