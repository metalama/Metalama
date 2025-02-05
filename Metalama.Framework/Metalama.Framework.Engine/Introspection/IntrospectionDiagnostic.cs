// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Introspection
{
    internal sealed class IntrospectionDiagnostic : IIntrospectionDiagnostic
    {
        private readonly Diagnostic _diagnostic;

        public ICompilation Compilation { get; }

        public IntrospectionDiagnostic( Diagnostic diagnostic, ICompilation compilation, IntrospectionDiagnosticSource source )
        {
            this._diagnostic = diagnostic;
            this.Compilation = compilation;
            this.Source = source;
        }

        public string Id => this._diagnostic.Id;

        public string Message => this._diagnostic.GetLocalizedMessage();

        public string? FilePath => this._diagnostic.Location.SourceTree?.FilePath;

        public int? Line => this._diagnostic.Location.GetLineSpan().StartLinePosition.Line;

        [Memo]
        public IDeclaration? Declaration => this.GetDeclaration();

        public Severity Severity => this._diagnostic.Severity.ToOurSeverity();

        public IntrospectionDiagnosticSource Source { get; }

        object? IIntrospectionDiagnostic.Details => null;

        private IDeclaration? GetDeclaration()
        {
            // Find the node reporting the error.
            var sourceTree = this._diagnostic.Location.SourceTree;

            if ( sourceTree == null )
            {
                return null;
            }

            var syntaxRoot = sourceTree.GetRoot();
            var node = syntaxRoot.FindNode( this._diagnostic.Location.SourceSpan );

            // Map to a symbol.
            var compilation = this.Compilation.GetRoslynCompilation();
            var semanticModel = compilation.GetCachedSemanticModel( sourceTree );

            for ( var n = node; n != null; n = n.Parent )
            {
                var symbol = semanticModel.GetDeclaredSymbol( n );

                if ( symbol != null )
                {
                    if ( this.Compilation.TryGetDeclaration( symbol, out var declaration ) )
                    {
                        return declaration;
                    }
                }
            }

            return null;
        }
    }
}