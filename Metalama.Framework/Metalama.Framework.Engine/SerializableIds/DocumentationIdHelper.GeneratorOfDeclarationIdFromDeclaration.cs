// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Text;

namespace Metalama.Framework.Engine.SerializableIds;

internal static partial class DocumentationIdHelper
{
    private sealed class GeneratorOfDeclarationIdFromDeclaration
    {
        private readonly StringBuilder _builder;
        private readonly Generator _generator;

        public GeneratorOfDeclarationIdFromDeclaration( StringBuilder builder )
        {
            this._builder = builder;
            this._generator = new Generator( builder );
        }

        public void Visit( IDeclaration declaration )
        {
            switch ( declaration )
            {
                case IEvent @event:
                    this._builder.Append( "E:" );
                    this._generator.Visit( @event );

                    break;

                case IField field:
                    this._builder.Append( "F:" );
                    this._generator.Visit( field );

                    break;

                case IPropertyOrIndexer property:
                    this._builder.Append( "P:" );
                    this._generator.Visit( property );

                    break;

                case IMethodBase method:
                    this._builder.Append( "M:" );
                    this._generator.Visit( method );

                    break;

                case INamespace ns:
                    this._builder.Append( "N:" );
                    this._generator.Visit( ns );

                    break;

                case INamedType namedType:
                    this._builder.Append( "T:" );
                    this._generator.Visit( namedType );

                    break;

                default:
                    throw new InvalidOperationException( $"Cannot generate a documentation comment id for symbol '{declaration}'." );
            }
        }

        private sealed class Generator
        {
            private readonly StringBuilder _builder;
            private GeneratorOfReferenceIdFromDeclaration? _referenceGenerator;

            public Generator( StringBuilder builder )
            {
                this._builder = builder;
            }

            private GeneratorOfReferenceIdFromDeclaration GetReferenceGenerator( IDeclaration typeParameterContext )
            {
                if ( this._referenceGenerator == null || !ReferenceEquals( this._referenceGenerator.TypeParameterContext, typeParameterContext ) )
                {
                    this._referenceGenerator = new GeneratorOfReferenceIdFromDeclaration( this._builder, typeParameterContext );
                }

                return this._referenceGenerator;
            }

            public void Visit( IEvent @event )
            {
                if ( this.Visit( @event.DeclaringType ) )
                {
                    this._builder.Append( '.' );
                }

                this._builder.Append( EncodeName( @event.Name ) );
            }

            public void Visit( IField field )
            {
                if ( this.Visit( field.DeclaringType ) )
                {
                    this._builder.Append( '.' );
                }

                this._builder.Append( EncodeName( field.Name ) );
            }

            public void Visit( IPropertyOrIndexer propertyOrIndexer )
            {
                if ( this.Visit( propertyOrIndexer.DeclaringType ) )
                {
                    this._builder.Append( '.' );
                }

                var name = EncodePropertyName( propertyOrIndexer.Name );
                this._builder.Append( EncodeName( name ) );

                if ( propertyOrIndexer is IIndexer indexer )
                {
                    this.AppendParameters( indexer.Parameters );
                }
            }

            public void Visit( IMethodBase methodBase )
            {
                if ( this.Visit( methodBase.DeclaringType ) )
                {
                    this._builder.Append( '.' );
                }

                this._builder.Append( EncodeName( methodBase.Name ) );

                if ( methodBase is IMethod { TypeParameters.Count: > 0 } method )
                {
                    this._builder.Append( "``" );
                    this._builder.Append( method.TypeParameters.Count );
                }

                this.AppendParameters( methodBase.Parameters );

                if ( methodBase is IMethod method2 && !method2.ReturnType.Equals( SpecialType.Void ) )
                {
                    this._builder.Append( '~' );
                    this.GetReferenceGenerator( method2 ).Visit( method2.ReturnType );
                }
            }

            private void AppendParameters( IParameterList parameters )
            {
                if ( parameters.Count > 0 )
                {
                    this._builder.Append( '(' );

                    for ( int i = 0, n = parameters.Count; i < n; i++ )
                    {
                        if ( i > 0 )
                        {
                            this._builder.Append( ',' );
                        }

                        var p = parameters[i];
                        this.GetReferenceGenerator( p.DeclaringMember ).Visit( p.Type );

                        if ( p.RefKind != RefKind.None )
                        {
                            this._builder.Append( '@' );
                        }
                    }

                    this._builder.Append( ')' );
                }
            }

            public bool Visit( INamespace ns )
            {
                if ( ns.IsGlobalNamespace )
                {
                    return false;
                }

                if ( this.Visit( ns.ContainingNamespace! ) )
                {
                    this._builder.Append( '.' );
                }

                this._builder.Append( EncodeName( ns.Name ) );

                return true;
            }

            public bool Visit( INamedType namedType )
            {
                var success = namedType.ContainingDeclaration is INamedType containingType
                    ? this.Visit( containingType )
                    : this.Visit( namedType.ContainingNamespace );

                if ( success )
                {
                    this._builder.Append( '.' );
                }

                this._builder.Append( EncodeName( namedType.Name ) );

                if ( namedType.TypeParameters.Count > 0 )
                {
                    this._builder.Append( '`' );
                    this._builder.Append( namedType.TypeParameters.Count );
                }

                return true;
            }
        }
    }
}