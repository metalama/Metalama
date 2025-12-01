// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a generic declaration with type parameters, common to <see cref="INamedType"/> and <see cref="IMethod"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Metalama, and unlike <c>System.Reflection</c>, generic types and methods are always fully bound. In generic declarations,
    /// such as in <c>typeof(List&lt;&gt;)</c>, type parameters are bound to themselves, i.e. the content of the <see cref="TypeArguments"/> and <see cref="TypeParameters"/>
    /// properties are identical.
    /// </para>
    /// <para>
    /// Consider the type <c>List&lt;T&gt;</c>, where <c>T</c> is a type parameter. In the generic type instance <c>List&lt;int&gt;</c>,
    /// <c>T</c> is the type parameter and <c>int</c> is the type argument; the <c>T</c> parameter is bound to <c>int</c>.
    /// In the type definition <c>List&lt;T&gt;</c>, <c>T</c> is both the type parameter and the type argument, because <c>T</c> is bound to itself.
    /// The <see cref="IsCanonicalGenericInstance"/> property returns <c>true</c> when all type parameters are bound to themselves.
    /// </para>
    /// <para>
    /// To create a generic instance from a generic definition, use <see cref="INamedType.MakeGenericInstance"/> or the extension methods in <see cref="GenericExtensions"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="GenericExtensions"/>
    /// <seealso cref="INamedType"/>
    /// <seealso cref="IMethod"/>
    /// <seealso cref="ITypeParameter"/>
    /// <seealso cref="IGenericContext"/>
    /// <seealso href="@type-system"/>
    public interface IGeneric : IMemberOrNamedType
    {
        /// <summary>
        /// Gets the type parameters of the current type or method.
        /// </summary>
        ITypeParameterList TypeParameters { get; }

        /// <summary>
        /// Gets the generic type arguments of the current type or method, which are the type values
        /// applied to the <see cref="TypeParameters"/> of the current type. The number of items in this list is always the same
        /// as in <see cref="TypeParameters"/>. 
        /// </summary>
        /// <remarks>
        /// When reflecting a generic declaration, i.e. with unbound type parameters, the content
        /// of this collection is identical to <see cref="TypeParameters"/>. That is, there is no such thing as an unbound generic declaration
        /// in Metalama because generic declarations are bound to their parameters.
        /// </remarks>
        IReadOnlyList<IType> TypeArguments { get; }

        /// <summary>
        /// Gets a value indicating whether this member has type parameters, regardless the fact that the containing type, if any, is generic.
        /// </summary>
        bool IsGeneric { get; }

        /// <summary>
        /// Gets a value indicating whether all type parameters are bound to themselves, i.e. if the content of <see cref="TypeArguments"/> and <see cref="TypeParameters"/> are equal.
        /// This property returns <c>true</c> if the current declaration has no generic argument. For generic methods, this property returns <c>false</c> if the declaring type is generic but is not a canonical generic instance.
        /// </summary>
        bool IsCanonicalGenericInstance { get; }
    }
}