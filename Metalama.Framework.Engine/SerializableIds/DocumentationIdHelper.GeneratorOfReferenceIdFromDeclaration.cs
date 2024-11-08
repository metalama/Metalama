// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using System;
using System.Text;

namespace Metalama.Framework.Engine.SerializableIds;

internal static partial class DocumentationIdHelper
{
    private sealed class GeneratorOfReferenceIdFromDeclaration( StringBuilder builder, IDeclaration? typeParameterContext )
    {
        public IDeclaration? TypeParameterContext { get; } = typeParameterContext;

        private void BuildDottedName( INamedType namedType )
        {
            var success = namedType.ContainingDeclaration is INamedType containingType
                ? this.Visit( containingType )
                : this.Visit( namedType.ContainingNamespace );

            if ( success )
            {
                builder.Append( '.' );
            }

            builder.Append( EncodeName( namedType.Name ) );
        }

        private void BuildDottedName( INamespace ns )
        {
            if ( this.Visit( ns.ContainingNamespace.AssertNotNull() ) )
            {
                builder.Append( '.' );
            }

            builder.Append( EncodeName( ns.Name ) );
        }

        public void Visit( ICompilationElement compilationElement )
        {
            switch ( compilationElement )
            {
                case IType type:
                    this.Visit( type );

                    break;

                case INamespace @namespace:
                    this.Visit( @namespace );

                    break;

                default:
                    throw new AssertionFailedException();
            }
        }

        private bool Visit( INamespace ns )
        {
            if ( ns.IsGlobalNamespace )
            {
                return false;
            }

            this.BuildDottedName( ns );

            return true;
        }

        public void Visit( IType type )
        {
            switch ( type )
            {
                case INamedType namedType:
                    this.Visit( namedType );

                    return;

                case IDynamicType dynamicType:
                    this.Visit( dynamicType );

                    return;

                case IArrayType arrayType:
                    this.Visit( arrayType );

                    return;

                case IPointerType pointerType:
                    this.Visit( pointerType );

                    return;

                case ITypeParameter parameter:
                    this.Visit( parameter );

                    return;

                default:
                    throw new NotSupportedException( $"The type '{type}' is not supported" );
            }
        }

        private bool Visit( INamedType namedType )
        {
            this.BuildDottedName( namedType );

            if ( namedType.IsGeneric )
            {
                if ( namedType.IsCanonicalGenericInstance )
                {
                    builder.Append( '`' );
                    builder.Append( namedType.TypeParameters.Count );
                }
                else if ( namedType.TypeArguments.Count > 0 )
                {
                    builder.Append( '{' );

                    for ( int i = 0, n = namedType.TypeArguments.Count; i < n; i++ )
                    {
                        if ( i > 0 )
                        {
                            builder.Append( ',' );
                        }

                        this.Visit( namedType.TypeArguments[i] );
                    }

                    builder.Append( '}' );
                }
            }

            return true;
        }

        private void Visit( IDynamicType dynamicType )
        {
            _ = dynamicType;

            builder.Append( "System.Object" );
        }

        private void Visit( IArrayType arrayType )
        {
            this.Visit( arrayType.ElementType );

            builder.Append( '[' );

            for ( int i = 0, n = arrayType.Rank; i < n; i++ )
            {
                if ( i > 0 )
                {
                    builder.Append( ',' );
                }
            }

            builder.Append( ']' );
        }

        private void Visit( IPointerType pointerType )
        {
            this.Visit( pointerType.PointedAtType );
            builder.Append( '*' );
        }

        private void Visit( ITypeParameter typeParameter )
        {
            if ( !this.IsInScope( typeParameter ) )
            {
                // reference to type parameter not in scope, make explicit scope reference
                var declarer = new GeneratorOfDeclarationIdFromDeclaration( builder );
                declarer.Visit( typeParameter.ContainingDeclaration.AssertNotNull() );
                builder.Append( ':' );
            }

            if ( typeParameter.ContainingDeclaration is IMethod )
            {
                builder.Append( "``" );
                builder.Append( typeParameter.Index );
            }
            else
            {
                // get count of all type parameter preceding the declaration of the type parameters containing symbol.
                var container = typeParameter.ContainingDeclaration?.ContainingDeclaration;
                var b = GetTotalTypeParameterCount( container as INamedType );
                builder.Append( '`' );
                builder.Append( b + typeParameter.Index );
            }
        }

        private bool IsInScope( ITypeParameter typeParameter )
        {
            // determine if the type parameter is declared in scope defined by the typeParameterContext symbol
            var typeParameterDeclarer = typeParameter.ContainingDeclaration;

            for ( var scope = this.TypeParameterContext; scope != null; scope = scope.ContainingDeclaration )
            {
                if ( scope.Equals( typeParameterDeclarer ) )
                {
                    return true;
                }
            }

            return false;
        }
    }
}