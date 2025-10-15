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
        /// <c>implicit</c> conversion operator.
        /// </summary>
        ImplicitConversion,

        /// <summary>
        /// <c>explicit</c> conversion operator.
        /// </summary>
        ExplicitConversion,

        /// <summary>
        /// <c>+</c> operator.
        /// </summary>
        Addition,

        /// <summary>
        /// <c>&amp;</c> operator.
        /// </summary>
        BitwiseAnd,

        /// <summary>
        /// <c>|</c> operator.
        /// </summary>
        BitwiseOr,

        /// <summary>
        /// <c>--</c> operator.
        /// </summary>
        Decrement,

        /// <summary>
        /// <c>/</c> operator.
        /// </summary>
        Division,

        /// <summary>
        /// <c>==</c> operator.
        /// </summary>
        Equality,

        /// <summary>
        /// <c>^</c> operator.
        /// </summary>
        ExclusiveOr,

        /// <summary>
        /// <c>false</c> operator.
        /// </summary>
        False,

        /// <summary>
        /// <c>&gt;</c> operator.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// <c>&gt;=</c> operator.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// <c>++</c> operator.
        /// </summary>
        Increment,

        /// <summary>
        /// <c>!=</c> operator.
        /// </summary>
        Inequality,

        /// <summary>
        /// <c>&lt;&lt;</c> operator.
        /// </summary>
        LeftShift,

        /// <summary>
        /// <c>&lt;</c> operator.
        /// </summary>
        LessThan,

        /// <summary>
        /// <c>&lt;=</c> operator.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// <c>!</c> operator.
        /// </summary>
        LogicalNot,

        /// <summary>
        /// <c>%</c> operator.
        /// </summary>
        Modulus,

        /// <summary>
        /// <c>*</c> operator.
        /// </summary>
        Multiply,

        /// <summary>
        /// <c>~</c> operator.
        /// </summary>
        OnesComplement,

        /// <summary>
        /// <c>&gt;&gt;</c> operator.
        /// </summary>
        RightShift,

        /// <summary>
        /// <c>-</c> operator.
        /// </summary>
        Subtraction,

        /// <summary>
        /// <c>true</c> operator.
        /// </summary>
        True,

        /// <summary>
        /// <c>-</c> (unary negation) operator.
        /// </summary>
        UnaryNegation,

        /// <summary>
        /// <c>+</c> (unary plus) operator.
        /// </summary>
        UnaryPlus,

        /// <summary>
        /// <c>&gt;&gt;&gt;</c> operator (C# 11+).
        /// </summary>
        UnsignedRightShift,

        /// <summary>
        /// <c>+=</c> operator (C# 14+).
        /// </summary>
        AdditionAssignment,

        /// <summary>
        /// <c>-=</c> operator (C# 14+).
        /// </summary>
        SubtractionAssignment,
        
        /// <summary>
        /// <c>/=</c> operator (C# 14+).
        /// </summary>
        DivisionAssignment,

        /// <summary>
        /// <c>%=</c> operator (C# 14+).
        /// </summary>
        ModulusAssignment,

        /// <summary>
        /// <c>&amp;=</c> operator (C# 14+).
        /// </summary>
        BitwiseAndAssignment,

        /// <summary>
        /// <c>|=</c> operator (C# 14+).
        /// </summary>
        BitwiseOrAssignment,

        /// <summary>
        /// <c>^=</c> operator (C# 14+).
        /// </summary>
        ExclusiveOrAssignment,

        /// <summary>
        /// <c>&lt;&lt;=</c> operator (C# 14+).
        /// </summary>
        LeftShiftAssignment,

        /// <summary>
        /// <c>&gt;&gt;=</c> operator (C# 14+).
        /// </summary>
        RightShiftAssignment,

        /// <summary>
        /// <c>&gt;&gt;&gt;=</c> operator (C# 14+).
        /// </summary>
        UnsignedRightShiftAssignment,

        /// <summary>
        /// <c>explicit checked</c> conversion operator.
        /// </summary>
        CheckedExplicitConversion,

        /// <summary>
        /// <c>checked +</c> operator.
        /// </summary>
        CheckedAddition,

        /// <summary>
        /// <c>checked --</c> operator.
        /// </summary>
        CheckedDecrement,

        /// <summary>
        /// <c>checked /</c> operator.
        /// </summary>
        CheckedDivision,

        /// <summary>
        /// <c>checked ++</c> operator.
        /// </summary>
        CheckedIncrement,

        /// <summary>
        /// <c>checked *</c> operator.
        /// </summary>
        CheckedMultiply,

        /// <summary>
        /// <c>checked -</c> operator.
        /// </summary>
        CheckedSubtraction,

        /// <summary>
        /// <c>checked -</c> (unary negation) operator.
        /// </summary>
        CheckedUnaryNegation,

        /// <summary>
        /// <c>||</c> operator.
        /// </summary>
        LogicalOr,

        /// <summary>
        /// <c>&amp;&amp;</c> operator.
        /// </summary>
        LogicalAnd,

        /// <summary>
        /// <c>&amp;</c> (concatenate) operator.
        /// </summary>
        Concatenate,

        /// <summary>
        /// <c>^</c> (exponent) operator.
        /// </summary>
        Exponent,

        /// <summary>
        /// <c>/</c> (integer division) operator.
        /// </summary>
        IntegerDivision,

        /// <summary>
        /// <c>Like</c> operator.
        /// </summary>
        Like,

        /// <summary>
        /// <c>*=</c> operator.
        /// </summary>
        MultiplicationAssignment,

        /// <summary>
        /// <c>++=</c> operator.
        /// </summary>
        IncrementAssignment,

        /// <summary>
        /// <c>--=</c> operator.
        /// </summary>
        DecrementAssignment,

        /// <summary>
        /// <c>checked +=</c> operator.
        /// </summary>
        CheckedAdditionAssignment,

        /// <summary>
        /// <c>checked -=</c> operator.
        /// </summary>
        CheckedSubtractionAssignment,

        /// <summary>
        /// <c>checked *=</c> operator.
        /// </summary>
        CheckedMultiplicationAssignment,

        /// <summary>
        /// <c>checked /=</c> operator.
        /// </summary>
        CheckedDivisionAssignment,

        /// <summary>
        /// <c>checked ++=</c> operator.
        /// </summary>
        CheckedIncrementAssignment,

        /// <summary>
        /// <c>checked --=</c> operator.
        /// </summary>
        CheckedDecrementAssignment
    }
}