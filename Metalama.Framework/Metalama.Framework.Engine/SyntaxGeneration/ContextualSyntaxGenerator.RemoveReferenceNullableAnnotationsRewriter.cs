// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.SyntaxGeneration
{
    public partial class ContextualSyntaxGenerator
    {
        private sealed class RemoveReferenceNullableAnnotationsRewriterForSymbol : SafeSyntaxRewriter
        {
            private ITypeSymbol _type;

            public RemoveReferenceNullableAnnotationsRewriterForSymbol( ITypeSymbol type )
            {
                this._type = type;
            }

            private INamedTypeSymbol GetExactTypeInNestedType( string name )
            {
                for ( var t = (INamedTypeSymbol) this._type; t != null!; t = t.ContainingType )
                {
                    if ( t.Name == name )
                    {
                        return t;
                    }
                }

                throw new AssertionFailedException( $"Cannot find type '{name}' in '{this._type}'" );
            }

            public override SyntaxNode VisitGenericName( GenericNameSyntax node )
            {
                var type = this.GetExactTypeInNestedType( node.Identifier.Text );

                var argumentsCount = node.TypeArgumentList.Arguments.Count;
                var typeArguments = new TypeSyntax[argumentsCount];

                for ( var i = 0; i < argumentsCount; i++ )
                {
                    using ( this.WithType( type.TypeArguments[i] ) )
                    {
                        typeArguments[i] = (TypeSyntax) this.Visit( node.TypeArgumentList.Arguments[i] )!;
                    }
                }

                return node.WithTypeArgumentList( SyntaxFactory.TypeArgumentList( SyntaxFactory.SeparatedList( typeArguments ) ) );
            }

            public override SyntaxNode? VisitArrayType( ArrayTypeSyntax node )
            {
                var type = (IArrayTypeSymbol) this._type;

                using ( this.WithType( type.ElementType ) )
                {
                    return base.VisitArrayType( node );
                }
            }

            public override SyntaxNode VisitTupleType( TupleTypeSyntax node )
            {
                var type = (INamedTypeSymbol) this._type;

                if ( type.NullableAnnotation == NullableAnnotation.Annotated )
                {
                    type = (INamedTypeSymbol) type.TypeArguments[0];
                }

                var elements = new TupleElementSyntax[node.Elements.Count];

                for ( var i = 0; i < node.Elements.Count; i++ )
                {
                    using ( this.WithType( type.TupleElements[i].Type ) )
                    {
                        elements[i] = (TupleElementSyntax) this.VisitTupleElement( node.Elements[i] )!;
                    }
                }

                return node.WithElements( SyntaxFactory.SeparatedList( elements ) );
            }

            public override SyntaxNode VisitFunctionPointerType( FunctionPointerTypeSyntax node )
            {
                var type = (IFunctionPointerTypeSymbol) this._type;

                var parameters = new FunctionPointerParameterSyntax[node.ParameterList.Parameters.Count];

                var lastParameterIndex = node.ParameterList.Parameters.Count - 1;

                for ( var i = 0; i < node.ParameterList.Parameters.Count; i++ )
                {
                    var parameterType = i == lastParameterIndex ? type.Signature.ReturnType : type.Signature.Parameters[i].Type;

                    using ( this.WithType( parameterType ) )
                    {
                        parameters[i] = (FunctionPointerParameterSyntax) this.VisitFunctionPointerParameter( node.ParameterList.Parameters[i] )!;
                    }
                }

                return node.WithParameterList( SyntaxFactory.FunctionPointerParameterList( SyntaxFactory.SeparatedList( parameters ) ) );
            }

            public override SyntaxNode? VisitNullableType( NullableTypeSyntax node )
            {
                if ( this._type.IsReferenceType )
                {
                    // Skip the annotation.
                    return this.Visit( node.ElementType );
                }
                else
                {
                    // Keep it.
                    var type = (INamedTypeSymbol) this._type;

                    using ( this.WithType( type.TypeArguments.Single() ) )
                    {
                        return base.VisitNullableType( node );
                    }
                }
            }

            private WithTypeCookie WithType( ITypeSymbol type )
            {
                var cookie = new WithTypeCookie( this, this._type );
                this._type = type;

                return cookie;
            }

            private readonly struct WithTypeCookie : IDisposable
            {
                private readonly RemoveReferenceNullableAnnotationsRewriterForSymbol _parent;
                private readonly ITypeSymbol _oldType;

                public WithTypeCookie( RemoveReferenceNullableAnnotationsRewriterForSymbol parent, ITypeSymbol oldType )
                {
                    this._parent = parent;
                    this._oldType = oldType;
                }

                public void Dispose() => this._parent._type = this._oldType;
            }
        }

        private sealed class RemoveReferenceNullableAnnotationsRewriter : SafeSyntaxRewriter
        {
            private IType _type;

            public RemoveReferenceNullableAnnotationsRewriter( IType type )
            {
                this._type = type;
            }

            private INamedType GetExactTypeInNestedType( string name )
            {
                for ( var t = (INamedType) this._type; t != null!; t = t.DeclaringType )
                {
                    if ( t.Name == name )
                    {
                        return t;
                    }
                }

                throw new AssertionFailedException( $"Cannot find type '{name}' in '{this._type}'" );
            }

            public override SyntaxNode VisitGenericName( GenericNameSyntax node )
            {
                var type = this.GetExactTypeInNestedType( node.Identifier.Text );

                var argumentsCount = node.TypeArgumentList.Arguments.Count;
                var typeArguments = new TypeSyntax[argumentsCount];

                for ( var i = 0; i < argumentsCount; i++ )
                {
                    using ( this.WithType( type.TypeArguments[i] ) )
                    {
                        typeArguments[i] = (TypeSyntax) this.Visit( node.TypeArgumentList.Arguments[i] )!;
                    }
                }

                return node.WithTypeArgumentList( SyntaxFactory.TypeArgumentList( SyntaxFactory.SeparatedList( typeArguments ) ) );
            }

            public override SyntaxNode? VisitArrayType( ArrayTypeSyntax node )
            {
                var type = (IArrayType) this._type;

                using ( this.WithType( type.ElementType ) )
                {
                    return base.VisitArrayType( node );
                }
            }

            public override SyntaxNode VisitTupleType( TupleTypeSyntax node )
            {
                // tuple elements are not representable as IType
                var typeSymbol = this._type.GetSymbol().AssertSymbolNotNull();

                return new RemoveReferenceNullableAnnotationsRewriterForSymbol( typeSymbol ).Visit( node )!;
            }

            public override SyntaxNode VisitFunctionPointerType( FunctionPointerTypeSyntax node )
            {
                // function pointer types are basically not representable as IType
                var typeSymbol = this._type.GetSymbol().AssertSymbolNotNull();

                return new RemoveReferenceNullableAnnotationsRewriterForSymbol( typeSymbol ).Visit( node )!;
            }

            public override SyntaxNode? VisitNullableType( NullableTypeSyntax node )
            {
                if ( this._type.IsReferenceType == true )
                {
                    // Skip the annotation.
                    return this.Visit( node.ElementType );
                }
                else
                {
                    // Keep it.
                    var type = (INamedType) this._type;

                    using ( this.WithType( type.TypeArguments.Single() ) )
                    {
                        return base.VisitNullableType( node );
                    }
                }
            }

            private WithTypeCookie WithType( IType type )
            {
                var cookie = new WithTypeCookie( this, this._type );
                this._type = type;

                return cookie;
            }

            private readonly struct WithTypeCookie : IDisposable
            {
                private readonly RemoveReferenceNullableAnnotationsRewriter _parent;
                private readonly IType _oldType;

                public WithTypeCookie( RemoveReferenceNullableAnnotationsRewriter parent, IType oldType )
                {
                    this._parent = parent;
                    this._oldType = oldType;
                }

                public void Dispose() => this._parent._type = this._oldType;
            }
        }
    }
}