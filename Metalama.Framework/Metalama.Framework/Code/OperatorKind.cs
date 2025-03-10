// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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
        UnaryPlus
    }
}