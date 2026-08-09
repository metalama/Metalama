// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Describes conversion between types possible during comparison.
    /// </summary>
    /// <seealso cref="IType"/>
    [CompileTime]
    public enum ConversionKind
    {
        /// <summary>
        /// Accepts any value that is type-compatible, including boxing conversions, but excluding user-defined implicit operators. This corresponds to the behavior of C# <c>is</c> operator.
        /// </summary>
        Default,

        /// <summary>
        /// Accepts any value that is reference-compatible with the given type i.e. instances of subclasses or interface implementations, but refuses boxing conversions.
        /// </summary>
        Reference,

        /// <summary>
        /// Accepts any value implicitly convertible to the given type, including boxing and user-defined implicit operators. This corresponds to C# value assignability.
        /// </summary>
        Implicit,

        /// <summary>
        /// Accepts any value that extends or implements a type that is of the same type definition as the given type definition.
        /// </summary>
        /// <remarks>
        /// For non-generic types behaves like <see cref="Reference"/>. For generic types, ignores all type arguments and tests that 
        /// the given type definition is equal to a type definition of the value, a type definition of any base type, or a type definition of
        /// any implemented interface (by the type itself or by any base type, including base interfaces).
        /// </remarks>
        TypeDefinition,

        /// <summary>
        /// Accepts only the types between which the language defines an identity conversion. As in the language, the
        /// nullability of a reference type and the names of the elements of a tuple do not take part, so
        /// <c>object</c> and <c>object?</c>, and <c>(int A, int B)</c> and <c>(int, int)</c>, are accepted.
        /// </summary>
        /// <remarks>
        /// This is the comparison to use to match a signature, because overload resolution ignores both. Use
        /// <see cref="Equal"/> to require the types to be equal, which is a stricter question and the one the
        /// comparers of <see cref="Comparers.ICompilationComparers"/> answer.
        /// </remarks>
        Identical,

        /// <summary>
        /// Accepts only the types that are equal, as the comparer that is being used defines equality. Unlike
        /// <see cref="Identical"/>, the names of the elements of a tuple take part, and the nullability of a
        /// reference type takes part when the comparer is <see cref="Comparers.ICompilationComparers.IncludeNullability"/>.
        /// </summary>
        Equal,

        [Obsolete( "Use Reference.", true )]
        ImplicitReference = Reference
    }
}