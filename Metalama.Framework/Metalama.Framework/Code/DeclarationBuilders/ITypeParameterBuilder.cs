// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    public interface ITypeParameterBuilder : IDeclarationBuilder, ITypeParameter
    {
        new bool? IsConstraintNullable { get; set; }

        new string Name { get; set; }

        new TypeKindConstraint TypeKindConstraint { get; set; }

        new bool AllowsRefStruct { get; set; }

        new VarianceKind Variance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the generic parameter has the <c>new()</c> constraint.
        /// </summary>
        new bool HasDefaultConstructorConstraint { get; set; }

        void AddTypeConstraint( IType type );

        void AddTypeConstraint( Type type );
    }
}