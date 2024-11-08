// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Visitors
{
    /// <summary>
    /// A rewriter that allows to rewrite instances of the <see cref="IType"/> interface.
    /// </summary>
    internal abstract class TypeRewriter
    {
        [PublicAPI]
        protected static TypeRewriter Null { get; } = new NullRewriter();

        internal virtual IType Visit( IArrayType arrayType )
        {
            var elementType = this.Visit( arrayType.ElementType );

            if ( ReferenceEquals( elementType, arrayType.ElementType ) )
            {
                return arrayType;
            }
            else if ( arrayType is SymbolArrayType sourceArrayType )
            {
                return sourceArrayType.WithElementType( elementType );
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public virtual IType Visit( IType elementType ) => ((ITypeImpl) elementType).Accept( this );

        internal virtual IType Visit( IDynamicType dynamicType ) => dynamicType;

        internal virtual IType Visit( IPointerType pointerType )
        {
            var pointedAtType = this.Visit( pointerType.PointedAtType );

            if ( ReferenceEquals( pointedAtType, pointerType.PointedAtType ) )
            {
                return pointerType;
            }
            else if ( pointerType is SymbolPointerType sourcePointerType )
            {
                return sourcePointerType.WithPointedAtType( pointedAtType );
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal virtual IType Visit( IFunctionPointerType functionPointerType )
        {
            throw new NotImplementedException( "Function pointers are not fully supported." );
        }

        internal virtual IType Visit( INamedType namedType )
        {
            if ( !namedType.IsGeneric )
            {
                return namedType;
            }

            var hasChange = false;
            INamedType typeDefinition;

            if ( namedType.DeclaringType != null )
            {
                var mappedDeclaringType = (INamedType) this.Visit( namedType.DeclaringType );
                hasChange |= !ReferenceEquals( mappedDeclaringType, namedType.DeclaringType );
                typeDefinition = mappedDeclaringType.Types.OfName( namedType.Name ).Single( t => t.TypeParameters.Count == namedType.TypeParameters.Count );

                if ( namedType.TypeArguments.Count == 0 )
                {
                    return typeDefinition;
                }
            }
            else
            {
                typeDefinition = namedType;
            }

            var typeArguments = new IType[namedType.TypeArguments.Count];

            for ( var index = 0; index < namedType.TypeArguments.Count; index++ )
            {
                var t = namedType.TypeArguments[index];
                var argumentType = this.Visit( t );
                hasChange |= !ReferenceEquals( argumentType, t );
                typeArguments[index] = argumentType;
            }

            if ( hasChange )
            {
                if ( typeDefinition is SourceNamedType sourceNamedType )
                {
                    return sourceNamedType.WithTypeArguments( typeArguments );
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                return namedType;
            }
        }

        internal virtual IType Visit( ITypeParameter typeParameter ) => typeParameter;

        private sealed class NullRewriter : TypeRewriter
        {
            public override IType Visit( IType elementType ) => elementType;

            internal override IType Visit( IArrayType arrayType ) => arrayType;

            internal override IType Visit( IDynamicType dynamicType ) => dynamicType;

            internal override IType Visit( IPointerType pointerType ) => pointerType;

            internal override IType Visit( INamedType namedType ) => namedType;

            internal override IType Visit( ITypeParameter typeParameter ) => typeParameter;

            internal override IType Visit( IFunctionPointerType functionPointerType ) => functionPointerType;
        }
    }
}