// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeCompilationBuilder
{
    private sealed class EmbeddedAttributeDetector : SafeSyntaxVisitor
    {
        private readonly Action<SyntaxNode> _onEmbeddedAttributeDeclarationsFound;
        private readonly Stack<string> _currentNamespaceStack = new();
        private string _currentNamespace = "";

        public EmbeddedAttributeDetector( Action<SyntaxNode> onEmbeddedAttributeDeclarationsFound )
        {
            this._onEmbeddedAttributeDeclarationsFound = onEmbeddedAttributeDeclarationsFound;
        }

        private void SetCurrentNamespace() => this._currentNamespace = string.Join( ".", this._currentNamespaceStack.Reverse() );

        public override void VisitFileScopedNamespaceDeclaration( FileScopedNamespaceDeclarationSyntax node )
        {
            this._currentNamespaceStack.Push( node.Name.ToString() );
            this.SetCurrentNamespace();
        }

        public override void VisitNamespaceDeclaration( NamespaceDeclarationSyntax node )
        {
            this._currentNamespaceStack.Push( node.Name.ToString() );
            this.SetCurrentNamespace();

            foreach ( var member in node.Members )
            {
                this.Visit( member );
            }

            this._currentNamespaceStack.Pop();
            this.SetCurrentNamespace();
        }

        public override void VisitClassDeclaration( ClassDeclarationSyntax node )
        {
            if ( node.Identifier.Text == nameof(EmbeddedAttribute) && this._currentNamespace == "Microsoft.CodeAnalysis" )
            {
                this._onEmbeddedAttributeDeclarationsFound( node );
            }
        }

        public override void VisitCompilationUnit( CompilationUnitSyntax node )
        {
            foreach ( var member in node.Members )
            {
                this.Visit( member );
            }
        }
    }
}