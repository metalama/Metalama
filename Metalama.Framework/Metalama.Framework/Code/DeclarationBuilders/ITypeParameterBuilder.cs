// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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