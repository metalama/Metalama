// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Specifies whether a type parameter belongs to a type or a method.
    /// </summary>
    /// <seealso cref="ITypeParameter"/>
    public enum TypeParameterKind
    {
        /// <summary>
        /// The type parameter belongs to a type (class, struct, interface, etc.).
        /// </summary>
        Type,

        /// <summary>
        /// The type parameter belongs to a method.
        /// </summary>
        Method
    }

    /// <summary>
    /// Represents a generic type parameter of a method or type, such as <c>T</c> in <c>List&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type parameters are placeholders for types that are specified when a generic type or method is instantiated.
    /// They can have constraints that restrict which types can be used as type arguments.
    /// </para>
    /// <para>
    /// In Metalama, type parameters are always bound. In a generic definition like <c>List&lt;T&gt;</c>,
    /// the type parameter <c>T</c> is bound to itself. In a constructed generic type like <c>List&lt;int&gt;</c>,
    /// the type parameter <c>T</c> is bound to <c>int</c>. Use the <see cref="ResolvedType"/> property to
    /// get the type to which a type parameter is bound.
    /// </para>
    /// <para>
    /// Type parameters can have various constraints specified through properties like <see cref="TypeKindConstraint"/>,
    /// <see cref="TypeConstraints"/>, <see cref="HasDefaultConstructorConstraint"/>, and <see cref="AllowsRefStruct"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IGeneric"/>
    /// <seealso cref="INamedType"/>
    /// <seealso cref="IMethod"/>
    /// <seealso cref="IType"/>
    /// <seealso cref="TypeKindConstraint"/>
    /// <seealso href="@type-system"/>
    public interface ITypeParameter : INamedDeclaration, IType
    {
        /// <summary>
        /// Gets the zero-based position of the type parameter in the type parameter list.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the type constraints of the type parameter, such as base class or interface constraints.
        /// </summary>
        IReadOnlyList<IType> TypeConstraints { get; }

        /// <summary>
        /// Gets the constraint on the kind of type, e.g. <see cref="TypeKindConstraint.Class"/> or <see cref="TypeKindConstraint.Struct"/>.
        /// </summary>
        TypeKindConstraint TypeKindConstraint { get; }

        /// <summary>
        /// Gets a value indicating whether the type parameter has the <c>allows ref struct</c> anti-constraint.
        /// </summary>
        bool AllowsRefStruct { get; }

        /// <summary>
        /// Gets the kind variance: <see cref="VarianceKind.In"/>, <see cref="VarianceKind.Out"/> or <see cref="VarianceKind.None"/>.
        /// </summary>
        VarianceKind Variance { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="TypeKindConstraint.Class"/> constraint has the nullable annotation (?).
        /// This property returns <c>null</c> if the <see cref="TypeKindConstraint"/> has a different value than <see cref="TypeKindConstraint.Class"/>
        /// or if the nullability of the type parameter has not been analyzed.
        /// </summary>
        bool? IsConstraintNullable { get; }

        /// <summary>
        /// Gets a value indicating whether the type parameter has the <c>new()</c> constraint.
        /// </summary>
        bool HasDefaultConstructorConstraint { get; }

        /// <inheritdoc cref="IDeclaration.ToRef"/>
        new IRef<ITypeParameter> ToRef();

        /// <summary>
        /// Gets the concrete <see cref="IType"/> to which the type parameter is resolved, if the parent type or method
        /// is a generic instance. If it is a generic definition, returns the current instance.
        /// </summary>
        IType ResolvedType { get; }

        TypeParameterKind TypeParameterKind { get; }

        /// <inheritdoc cref="IType.ToNonNullable"/>
        new ITypeParameter ToNonNullable();

        // Note that ToNullable, when called with T : struct, can return the INamedType Nullable<T> and therefore cannot be cast to an ITypeParameter.
    }
}