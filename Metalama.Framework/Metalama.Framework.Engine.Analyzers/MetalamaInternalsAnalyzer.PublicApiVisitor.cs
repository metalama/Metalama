// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace Metalama.Framework.Engine.Analyzers;

public partial class MetalamaInternalsAnalyzer
{
    private sealed class PublicApiVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModelAnalysisContext _context;

        public PublicApiVisitor( SemanticModelAnalysisContext context )
        {
            this._context = context;
        }

        public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitDeclaration( node, base.VisitClassDeclaration );

        public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
            => this.VisitDeclaration(
                node,
                n =>
                {
                    this.Visit( n.ReturnType );
                    this.Visit( n.ParameterList );
                    this.Visit( n.TypeParameterList );
                } );

        public override void VisitPropertyDeclaration( PropertyDeclarationSyntax node )
            => this.VisitDeclaration(
                node,
                n => this.Visit( n.Type ) );

        private void VisitDeclaration<T>( T node, Action<T> visitDeeper ) where T : SyntaxNode
        {
            var symbol = this._context.SemanticModel.GetDeclaredSymbol( node );

            if ( symbol is { DeclaredAccessibility: Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal } )
            {
                visitDeeper( node );
            }
        }

        public override void VisitIdentifierName( IdentifierNameSyntax node )
        {
            var symbol = this._context.SemanticModel.GetSymbolInfo( node ).Symbol;

            if ( symbol?.ContainingAssembly?.Name == null )
            {
                return;
            }

            var referencedProjectKind = ProjectClassifier.GetProjectKind( symbol.ContainingAssembly.Name );

            if ( referencedProjectKind == ProjectKind.MetalamaInternal )
            {
                this._context.ReportDiagnostic( Diagnostic.Create( _cannotExposeApi, node.GetLocation(), symbol.ToDisplayString() ) );
            }
        }

        public override void VisitFieldDeclaration( FieldDeclarationSyntax node )
        {
            foreach ( var declarator in node.Declaration.Variables )
            {
                this.VisitDeclaration( declarator, _ => this.Visit( node.Declaration.Type ) );
            }
        }

        public override void VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
        {
            foreach ( var declarator in node.Declaration.Variables )
            {
                this.VisitDeclaration( declarator, _ => this.Visit( node.Declaration.Type ) );
            }
        }

        public override void VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
            => this.VisitDeclaration(
                node,
                c => this.Visit( c.ParameterList ) );

        public override void VisitEventDeclaration( EventDeclarationSyntax node ) => this.VisitDeclaration( node, n => this.Visit( n.Type ) );

        public override void VisitUsingDirective( UsingDirectiveSyntax node ) { }
    }
}