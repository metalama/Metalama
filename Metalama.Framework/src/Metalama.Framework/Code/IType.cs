// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using System;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a constructed type, for instance an array, a generic type instance, a pointer.
    /// A class, struct, enum or delegate are represented as an <see cref="INamedType"/>, which
    /// derive from <see cref="IType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Obtaining IType instances:</b>
    /// You can obtain an <see cref="IType"/> instance using <see cref="TypeFactory"/>, or by constructing derived types from an existing
    /// <see cref="IType"/> using methods like <see cref="MakeArrayType"/>, <see cref="MakePointerType"/>, <see cref="ToNullable"/>, and <see cref="ToNonNullable"/>.
    /// </para>
    /// <para>
    /// <b>Comparing types:</b>
    /// The <see cref="IType"/> interface implements <see cref="IEquatable{T}"/>. The implementation uses the <see cref="ICompilationComparers.Default"/> comparer.
    /// To use a different comparer, choose a different comparer from <see cref="IDeclaration"/>.<see cref="ICompilationElement.Compilation"/>.<see cref="ICompilation.Comparers"/>.
    /// You can also use <see cref="Equals(IType,TypeComparison)"/> and specify a <see cref="TypeComparison"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="TypeExtensions"/>
    /// <seealso cref="INamedType"/>
    /// <seealso cref="IArrayType"/>
    /// <seealso cref="IPointerType"/>
    /// <seealso cref="TypeKind"/>
    /// <seealso cref="SpecialType"/>
    /// <seealso cref="TypeComparison"/>
    /// <seealso cref="TypeFactory"/>
    /// <seealso href="@type-system"/>
    [CompileTime]
    public interface IType : ICompilationElement, IDisplayable, IEquatable<IType>
    {
        /// <summary>
        /// Converts the current <see cref="IType"/> to an <see cref="IRef{T}"/> that can be used to reference this type later,
        /// even after the type might have been modified by aspects.
        /// </summary>
        IRef<IType> ToRef();

        /// <summary>
        /// Gets the kind of type.
        /// </summary>
        TypeKind TypeKind { get; }

        /// <summary>
        /// Gets the <see cref="Code.SpecialType"/> enumeration value for the current type. Provides a fast way to determine whether
        /// the current type is of a well-known type. 
        /// </summary>
        SpecialType SpecialType { get; }

        /// <summary>
        /// Gets a reflection <see cref="Type"/> that represents the current type at run time.
        /// </summary>
        /// <returns>A <see cref="Type"/> that can be used only in run-time code.</returns>
        /// <seealso href="@reflection"/>
        [CompileTimeReturningRunTime]
        Type ToType();

        /// <summary>
        /// Gets a value indicating whether the type is a reference type. If the type is a generic parameter
        /// without a <c>struct</c>, <c>class</c> or similar constraint, this property evaluates to <c>null</c>.
        /// </summary>
        bool? IsReferenceType { get; }

        /// <summary>
        /// Gets the nullability of the type, or <c>null</c> if the type is a reference type but its nullability has not been analyzed or specified.
        /// This property returns <c>false</c> for normal value types and <c>true</c> for the <see cref="Nullable{T}"/> type. Note that in
        /// case of nullable value types, the current type represents the <see cref="Nullable{T}"/> type itself, and the inner value type
        /// is exposed as <see cref="INamedType.UnderlyingType"/>.
        /// </summary>
        bool? IsNullable { get; }

        /// <summary>
        /// Determines whether the current type is equal to a well-known special type.
        /// </summary>
        bool Equals( SpecialType specialType );

        /// <summary>
        /// Determines whether the current type is equal to another <see cref="IType"/> using a specified <see cref="TypeComparison"/> mode.
        /// </summary>
        /// <param name="otherType">The type to compare with the current type.</param>
        /// <param name="typeComparison">The comparison mode to use.</param>
        bool Equals( IType? otherType, TypeComparison typeComparison );

        /// <summary>
        /// Determines whether the current type is equal to a reflection <see cref="Type"/> using a specified <see cref="TypeComparison"/> mode.
        /// </summary>
        /// <param name="otherType">The reflection type to compare with the current type.</param>
        /// <param name="typeComparison">The comparison mode to use.</param>
        bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default );

        /// <summary>
        /// Creates an array type whose element type is the current type.
        /// </summary>
        /// <param name="rank">Number of dimensions of the array.</param>
        IArrayType MakeArrayType( int rank = 1 );

        /// <summary>
        /// Creates a pointer type pointing at the current type.
        /// </summary>
        /// <returns></returns>
        IPointerType MakePointerType();

        /// <summary>
        /// Creates a nullable type from the current <see cref="IType"/>. If the current type is already nullable, returns the current type.
        /// If the type is a value type, returns a <see cref="Nullable{T}"/> of this type.
        /// </summary>
        IType ToNullable();

        /// <summary>
        /// Returns the non-nullable type from the current <see cref="IType"/>. If the current type is a non-nullable reference type, returns the current type.
        /// If the current type is a <see cref="Nullable{T}"/>, i.e. a nullable value type, returns the underlying type.
        /// </summary>
        /// <remarks>
        /// Note that for non-value type type parameters, this method strips the nullable annotation, if any,
        /// which means it returns a type whose <see cref="IType.IsNullable"/> property returns <see langword="null" />.
        /// This is because C# has no way to express non-nullability for type parameters.
        /// </remarks>
        IType ToNonNullable();
    }
}