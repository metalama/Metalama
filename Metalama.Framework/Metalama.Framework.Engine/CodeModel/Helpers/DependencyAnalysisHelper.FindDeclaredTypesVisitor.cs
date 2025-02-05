// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

public static partial class DependencyAnalysisHelper
{
    private sealed class FindDeclaredTypesVisitor : SafeSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly Action<INamedTypeSymbol> _addDeclaredType;

        public FindDeclaredTypesVisitor( SemanticModel semanticModel, Action<INamedTypeSymbol> addDeclaredType )
        {
            this._semanticModel = semanticModel;
            this._addDeclaredType = addDeclaredType;
        }

        private void VisitType( SyntaxNode node )
        {
            if ( this._semanticModel.GetDeclaredSymbol( node ) is INamedTypeSymbol type )
            {
                this._addDeclaredType( type );
            }

            // Also index nested types.
            if ( node is TypeDeclarationSyntax typeDeclaration )
            {
                foreach ( var child in typeDeclaration.Members )
                {
                    if ( child is BaseTypeDeclarationSyntax )
                    {
                        this.VisitType( child );
                    }
                }
            }
        }

        public override void VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitEnumDeclaration( EnumDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitDelegateDeclaration( DelegateDeclarationSyntax node ) => this.VisitType( node );
    }
}