// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a type parameter that has been created by an advice.
    /// </summary>
    /// <seealso cref="ITypeParameter"/>
    /// <seealso cref="IGeneric"/>
    /// <seealso cref="IMethodBuilder"/>
    /// <seealso cref="INamedTypeBuilder"/>
    /// <seealso href="@introducing-members"/>
    public interface ITypeParameterBuilder : IDeclarationBuilder, ITypeParameter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the constraint type is nullable.
        /// </summary>
        new bool? IsConstraintNullable { get; set; }

        /// <summary>
        /// Gets or sets the name of the type parameter.
        /// </summary>
        new string Name { get; set; }

        /// <summary>
        /// Gets or sets the type kind constraint (e.g., <c>class</c>, <c>struct</c>, <c>unmanaged</c>).
        /// </summary>
        new TypeKindConstraint TypeKindConstraint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the type parameter allows <c>ref struct</c> types.
        /// </summary>
        new bool AllowsRefStruct { get; set; }

        /// <summary>
        /// Gets or sets the variance kind (e.g., <c>in</c> for contravariance, <c>out</c> for covariance).
        /// </summary>
        new VarianceKind Variance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the generic parameter has the <c>new()</c> constraint.
        /// </summary>
        new bool HasDefaultConstructorConstraint { get; set; }

        /// <summary>
        /// Adds a type constraint to the type parameter.
        /// </summary>
        /// <param name="type">The type constraint to add.</param>
        void AddTypeConstraint( IType type );

        /// <summary>
        /// Adds a type constraint to the type parameter.
        /// </summary>
        /// <param name="type">The type constraint to add.</param>
        void AddTypeConstraint( Type type );
    }
}