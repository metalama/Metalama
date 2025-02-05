// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.Engine.Analyzers;

public partial class MetalamaInternalsAnalyzer
{
    private sealed class UserProjectVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModelAnalysisContext _context;

        public UserProjectVisitor( SemanticModelAnalysisContext context )
        {
            this._context = context;
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
                this._context.ReportDiagnostic( Diagnostic.Create( _cannotConsumeApi, node.GetLocation(), symbol.ToDisplayString() ) );
            }
        }
    }
}