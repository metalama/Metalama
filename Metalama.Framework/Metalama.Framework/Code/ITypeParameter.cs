// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    public enum TypeParameterKind
    {
        Type,
        Method
    }

    /// <summary>
    /// Represents a generic parameter of a method or type.
    /// </summary>
    public interface ITypeParameter : INamedDeclaration, IType
    {
        /// <summary>
        /// Gets the position of the generic parameter.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the type constraints of the generic parameter.
        /// </summary>
        IReadOnlyList<IType> TypeConstraints { get; }

        /// <summary>
        /// Gets the constraint on the kind of type, e.g. <see cref="Code.TypeKindConstraint.Class"/> or <see cref="Code.TypeKindConstraint.Struct"/>.
        /// </summary>
        TypeKindConstraint TypeKindConstraint { get; }

        /// <summary>
        /// Gets a value indicating whether the generic parameter has the <c>allows ref struct</c> anti-constraint.
        /// </summary>
        bool AllowsRefStruct { get; }

        /// <summary>
        /// Gets the kind variance: <see cref="VarianceKind.In"/>, <see cref="VarianceKind.Out"/> or <see cref="VarianceKind.None"/>.
        /// </summary>
        VarianceKind Variance { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Code.TypeKindConstraint.Class"/> constraint has the nullable annotation (?).
        /// This property returns <c>null</c> if the <see cref="TypeKindConstraint"/> has a different value than <see cref="Code.TypeKindConstraint.Class"/>
        /// or if the nullability of the generic parameter is not analyzed.
        /// </summary>
        bool? IsConstraintNullable { get; }

        /// <summary>
        /// Gets a value indicating whether the generic parameter has the <c>new()</c> constraint.
        /// </summary>
        bool HasDefaultConstructorConstraint { get; }

        new IRef<ITypeParameter> ToRef();

        /// <summary>
        /// Gets the concrete <see cref="IType"/> to which the type parameter is resolved, if the parent type or method
        /// is a generic instance. If it is a generic definition, returns the current instance.
        /// </summary>
        IType ResolvedType { get; }

        TypeParameterKind TypeParameterKind { get; }

        new ITypeParameter ToNonNullable();

        // Note that ToNullable, when called with T : struct, can return the INamedType Nullable<T> and therefore cannot be cast to an ITypeParameter.
    }
}