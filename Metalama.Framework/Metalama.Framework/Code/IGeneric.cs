// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// An interface, common to <see cref="INamedType"/> and <see cref="IMethod"/>, that represents a generic declaration, i.e. a declaration
    /// with type parameters. 
    /// </summary>
    /// <remarks>
    /// In Metalama, and unlike <c>System.Reflection</c>, generic types and methods are always fully bound. In generic declarations,
    /// such as in (<c>typeof(List&lt;&gt;)</c>, type parameters are bound to themselves, i.e. the content of the <see cref="TypeArguments"/> and <see cref="TypeParameters"/>
    /// properties are identical.
    /// </remarks>
    /// <seealso cref="GenericExtensions"/>
    public interface IGeneric : IMemberOrNamedType
    {
        /// <summary>
        /// Gets the generic parameters of the current type or method.
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