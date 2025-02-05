// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime
{
    internal sealed partial class CompileTimeCompilationBuilder
    {
        /// <summary>
        /// Determines if a syntax tree has compile-time code. The result is exposed in the <see cref="HasCompileTimeCode"/> property.
        /// </summary>
        private sealed class FindCompileTimeCodeVisitor : SafeSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly ISymbolClassifier _classifier;
            private readonly CancellationToken _cancellationToken;
            private readonly ImmutableArray<UsingDirectiveSyntax>.Builder _globalUsings = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();

            public bool HasCompileTimeCode { get; private set; }

            public ImmutableArray<UsingDirectiveSyntax> GlobalUsings => this._globalUsings.ToImmutable();

            public FindCompileTimeCodeVisitor( SemanticModel semanticModel, ISymbolClassifier classifier, CancellationToken cancellationToken )
            {
                this._semanticModel = semanticModel;
                this._classifier = classifier;
                this._cancellationToken = cancellationToken;
            }

            public override void VisitUsingDirective( UsingDirectiveSyntax node )
            {
                if ( node.GlobalKeyword.IsKind( SyntaxKind.GlobalKeyword ) )
                {
                    // We remove the trivia to make sure we don't have any preprocessor directive.
                    // Note that we use a ElasticLineFeed because we don't know where the `using` will be used.
                    this._globalUsings.Add( node.WithoutLeadingTrivia().WithTrailingTrivia( SyntaxFactory.ElasticLineFeed ) );
                }
            }

            private void VisitTypeDeclaration( SyntaxNode node )
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                if ( this.HasCompileTimeCode )
                {
                    // No need to do anything more.
                    return;
                }

                var declaredSymbol = (INamedTypeSymbol?) this._semanticModel.GetDeclaredSymbol( node );

                if ( declaredSymbol != null &&
                     this._classifier.GetTemplatingScope( declaredSymbol ) != TemplatingScope.RunTimeOnly &&
                     !SystemTypeDetector.IsSystemType( declaredSymbol ) )
                {
                    this.HasCompileTimeCode = true;
                }

                if ( node is TypeDeclarationSyntax typeWithMembers )
                {
                    foreach ( var childType in typeWithMembers.Members )
                    {
                        if ( childType is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax )
                        {
                            this.VisitTypeDeclaration( childType );
                        }
                    }
                }
            }

            public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

            public override void VisitEnumDeclaration( EnumDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

            public override void VisitDelegateDeclaration( DelegateDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

            public override void VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

            public override void VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

            public override void VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
        }
    }
}