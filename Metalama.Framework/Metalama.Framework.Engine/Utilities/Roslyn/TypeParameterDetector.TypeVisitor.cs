// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static partial class TypeParameterDetector
{
    private sealed class TypeVisitor : TypeVisitor<ITypeParameter?>
    {
        public static TypeVisitor Instance { get; } = new();

        private TypeVisitor() { }

        protected override ITypeParameter DefaultVisit( IType type ) => throw new NotImplementedException();

        protected override ITypeParameter? VisitArrayType( IArrayType arrayType ) => this.Visit( arrayType.ElementType );

        protected override ITypeParameter? VisitDynamicType( IDynamicType dynamicType ) => null;

        protected override ITypeParameter? VisitNamedType( INamedType namedType )
        {
            ITypeParameter? maxTypeParameter = null;

            foreach ( var typeArgument in namedType.TypeArguments )
            {
                var typeParameter = this.Visit( typeArgument );

                if ( typeParameter != null )
                {
                    if ( typeParameter.ContainingDeclaration!.DeclarationKind == DeclarationKind.Method )
                    {
                        return typeParameter;
                    }
                    else
                    {
                        maxTypeParameter = typeParameter;
                    }
                }
            }

            return maxTypeParameter;
        }

        protected override ITypeParameter? VisitPointerType( IPointerType pointerType ) => this.Visit( pointerType.PointedAtType );

        protected override ITypeParameter? VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => null;

        protected override ITypeParameter VisitTypeParameter( ITypeParameter typeParameter ) => typeParameter;
    }
}