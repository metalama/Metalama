// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

public static partial class DependencyAnalysisHelper
{
    private sealed class FindDeclaredAndAttributeTypesVisitor : SafeSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly Action<INamedTypeSymbol> _addDeclaredType;
        private readonly Action<INamedTypeSymbol> _addAttributeType;

        public FindDeclaredAndAttributeTypesVisitor(
            SemanticModel semanticModel,
            Action<INamedTypeSymbol> addDeclaredType,
            Action<INamedTypeSymbol> addAttributeType )
        {
            this._semanticModel = semanticModel;
            this._addDeclaredType = addDeclaredType;
            this._addAttributeType = addAttributeType;
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

        public override void VisitBlock( BlockSyntax node ) { }

        public override void VisitArrowExpressionClause( ArrowExpressionClauseSyntax node ) { }

        public override void VisitInterfaceDeclaration( InterfaceDeclarationSyntax node )
        {
            this.VisitType( node );
            base.VisitInterfaceDeclaration( node );
        }

        public override void VisitClassDeclaration( ClassDeclarationSyntax node )
        {
            this.VisitType( node );
            base.VisitClassDeclaration( node );
        }

        public override void VisitStructDeclaration( StructDeclarationSyntax node )
        {
            this.VisitType( node );
            base.VisitStructDeclaration( node );
        }

        public override void VisitRecordDeclaration( RecordDeclarationSyntax node )
        {
            this.VisitType( node );
            base.VisitRecordDeclaration( node );
        }

        public override void VisitEnumDeclaration( EnumDeclarationSyntax node )
        {
            this.VisitType( node );
            base.VisitEnumDeclaration( node );
        }

        public override void VisitDelegateDeclaration( DelegateDeclarationSyntax node )
        {
            this.VisitType( node );
            base.VisitDelegateDeclaration( node );
        }

        public override void VisitAttribute( AttributeSyntax node )
        {
            var attributeConstructor = this._semanticModel.GetSymbolInfo( node ).Symbol;

            if ( attributeConstructor == null )
            {
                return;
            }

            this._addAttributeType( attributeConstructor.ContainingType );
        }
    }
}