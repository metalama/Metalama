// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Kinds of operators.
    /// </summary>
    [CompileTime]
    public enum OperatorKind
    {
        /// <summary>
        /// Not an operator.
        /// </summary>
        None,

        /// <summary>
        /// Implicit (widening) conversion.
        /// </summary>
        ImplicitConversion,

        /// <summary>
        /// Explicit (narrowing) conversion.
        /// </summary>
        ExplicitConversion,

        /// <summary>
        /// Addition operator.
        /// </summary>
        Addition,

        /// <summary>
        /// BitwiseAnd operator.
        /// </summary>
        BitwiseAnd,

        /// <summary>
        /// BitwiseOr operator.
        /// </summary>
        BitwiseOr,

        /// <summary>
        /// Decrement operator.
        /// </summary>
        Decrement,

        /// <summary>
        /// Division operator.
        /// </summary>
        Division,

        /// <summary>
        /// Equality operator.
        /// </summary>
        Equality,

        /// <summary>
        /// ExclusiveOr operator.
        /// </summary>
        ExclusiveOr,

        /// <summary>
        /// False operator.
        /// </summary>
        False,

        /// <summary>
        /// GreaterThan operator.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// GreaterThanOrEqual operator.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Increment operator.
        /// </summary>
        Increment,

        /// <summary>
        /// Inequality operator.
        /// </summary>
        Inequality,

        /// <summary>
        /// LeftShift operator.
        /// </summary>
        LeftShift,

        /// <summary>
        /// LessThan operator.
        /// </summary>
        LessThan,

        /// <summary>
        /// LessThanOrEqual operator.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// LogicalNot operator.
        /// </summary>
        LogicalNot,

        /// <summary>
        /// Modulus operator.
        /// </summary>
        Modulus,

        /// <summary>
        /// Multiply operator.
        /// </summary>
        Multiply,

        /// <summary>
        /// OnesComplement operator.
        /// </summary>
        OnesComplement,

        /// <summary>
        /// RightShift operator.
        /// </summary>
        RightShift,

        /// <summary>
        /// Subtraction operator.
        /// </summary>
        Subtraction,

        /// <summary>
        /// True operator.
        /// </summary>
        True,

        /// <summary>
        /// UnaryNegation operator.
        /// </summary>
        UnaryNegation,

        /// <summary>
        /// UnaryPlus operator.
        /// </summary>
        UnaryPlus,

        /// <summary>
        /// UnsignedRightShift operator (C# 11+).
        /// </summary>
        UnsignedRightShift,

        /// <summary>
        /// AdditionAssignment operator (C# 14+).
        /// </summary>
        AdditionAssignment,

        /// <summary>
        /// SubtractionAssignment operator (C# 14+).
        /// </summary>
        SubtractionAssignment,

        /// <summary>
        /// MultiplyAssignment operator (C# 14+).
        /// </summary>
        MultiplyAssignment,

        /// <summary>
        /// DivisionAssignment operator (C# 14+).
        /// </summary>
        DivisionAssignment,

        /// <summary>
        /// ModulusAssignment operator (C# 14+).
        /// </summary>
        ModulusAssignment,

        /// <summary>
        /// BitwiseAndAssignment operator (C# 14+).
        /// </summary>
        BitwiseAndAssignment,

        /// <summary>
        /// BitwiseOrAssignment operator (C# 14+).
        /// </summary>
        BitwiseOrAssignment,

        /// <summary>
        /// ExclusiveOrAssignment operator (C# 14+).
        /// </summary>
        ExclusiveOrAssignment,

        /// <summary>
        /// LeftShiftAssignment operator (C# 14+).
        /// </summary>
        LeftShiftAssignment,

        /// <summary>
        /// RightShiftAssignment operator (C# 14+).
        /// </summary>
        RightShiftAssignment,

        /// <summary>
        /// UnsignedRightShiftAssignment operator (C# 14+).
        /// </summary>
        UnsignedRightShiftAssignment,

        /// <summary>
        /// CheckedExplicitConversion operator.
        /// </summary>
        CheckedExplicitConversion,

        /// <summary>
        /// CheckedAddition operator.
        /// </summary>
        CheckedAddition,

        /// <summary>
        /// CheckedDecrement operator.
        /// </summary>
        CheckedDecrement,

        /// <summary>
        /// CheckedDivision operator.
        /// </summary>
        CheckedDivision,

        /// <summary>
        /// CheckedIncrement operator.
        /// </summary>
        CheckedIncrement,

        /// <summary>
        /// CheckedMultiply operator.
        /// </summary>
        CheckedMultiply,

        /// <summary>
        /// CheckedSubtraction operator.
        /// </summary>
        CheckedSubtraction,

        /// <summary>
        /// CheckedUnaryNegation operator.
        /// </summary>
        CheckedUnaryNegation,

        /// <summary>
        /// LogicalOr operator.
        /// </summary>
        LogicalOr,

        /// <summary>
        /// LogicalAnd operator.
        /// </summary>
        LogicalAnd,

        /// <summary>
        /// UnsignedLeftShift operator.
        /// </summary>
        UnsignedLeftShift,

        /// <summary>
        /// Concatenate operator.
        /// </summary>
        Concatenate,

        /// <summary>
        /// Exponent operator.
        /// </summary>
        Exponent,

        /// <summary>
        /// IntegerDivision operator.
        /// </summary>
        IntegerDivision,

        /// <summary>
        /// Like operator.
        /// </summary>
        Like,

        /// <summary>
        /// MultiplicationAssignment operator (different from MultiplyAssignment).
        /// </summary>
        MultiplicationAssignment,

        /// <summary>
        /// IncrementAssignment operator.
        /// </summary>
        IncrementAssignment,

        /// <summary>
        /// DecrementAssignment operator.
        /// </summary>
        DecrementAssignment,

        /// <summary>
        /// CheckedAdditionAssignment operator.
        /// </summary>
        CheckedAdditionAssignment,

        /// <summary>
        /// CheckedSubtractionAssignment operator.
        /// </summary>
        CheckedSubtractionAssignment,

        /// <summary>
        /// CheckedMultiplicationAssignment operator.
        /// </summary>
        CheckedMultiplicationAssignment,

        /// <summary>
        /// CheckedDivisionAssignment operator.
        /// </summary>
        CheckedDivisionAssignment,

        /// <summary>
        /// CheckedIncrementAssignment operator.
        /// </summary>
        CheckedIncrementAssignment,

        /// <summary>
        /// CheckedDecrementAssignment operator.
        /// </summary>
        CheckedDecrementAssignment
    }
}