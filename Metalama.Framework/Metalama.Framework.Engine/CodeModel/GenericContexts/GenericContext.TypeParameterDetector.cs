// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

public partial class GenericContext
{
    /// <summary>
    /// Determines if a type signature contains a generic parameter.
    /// </summary>
    private sealed class TypeParameterDetector : TypeVisitor<bool>
    {
        private TypeParameterDetector() { }

        public static TypeParameterDetector Instance { get; } = new();

        protected override bool DefaultVisit( IType type ) => throw new NotImplementedException( $"Type kind not handled: {type.TypeKind}." );

        protected override bool VisitArrayType( IArrayType arrayType ) => this.Visit( arrayType.ElementType );

        protected override bool VisitDynamicType( IDynamicType dynamicType ) => false;

        protected override bool VisitNamedType( INamedType namedType )
            => namedType.TypeArguments.Any( this.Visit ) ||
               (namedType.DeclaringType != null && this.Visit( namedType.DeclaringType ));

        protected override bool VisitPointerType( IPointerType pointerType ) => this.Visit( pointerType.PointedAtType );

        protected override bool VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => false;

        protected override bool VisitTypeParameter( ITypeParameter typeParameter ) => true;
    }
}