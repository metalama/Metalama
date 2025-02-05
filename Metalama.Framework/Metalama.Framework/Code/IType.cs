// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    /// The <see cref="IType"/> interface implements <see cref="IEquatable{T}"/>. The implementation uses the <see cref="ICompilationComparers.Default"/> comparer.
    /// To use a different comparer, choose a different comparer from <see cref="IDeclaration"/>.<see cref="ICompilationElement.Compilation"/>.<see cref="ICompilation.Comparers"/>.
    /// You can also use <see cref="Equals(IType,TypeComparison)"/> and specify a <see cref="TypeComparison"/>.
    /// </remarks>
    /// <seealso cref="TypeExtensions"/>
    [CompileTime]
    public interface IType : ICompilationElement, IDisplayable, IEquatable<IType>
    {
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

        bool Equals( IType? otherType, TypeComparison typeComparison );

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