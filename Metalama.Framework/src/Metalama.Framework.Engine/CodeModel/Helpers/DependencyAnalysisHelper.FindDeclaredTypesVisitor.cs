// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            var declaredSymbol = this._semanticModel.GetDeclaredSymbol( node );

            if ( declaredSymbol?.Kind == SymbolKind.NamedType && declaredSymbol is INamedTypeSymbol type )
            {
                this._addDeclaredType( type );
            }

            // Also index nested types.
            if ( node.SyntaxKind.IsTypeDeclaration && node is TypeDeclarationSyntax typeDeclaration )
            {
                foreach ( var child in typeDeclaration.Members )
                {
                    if ( child.SyntaxKind.IsBaseTypeDeclaration && child is BaseTypeDeclarationSyntax )
                    {
                        this.VisitType( child );
                    }
                }
            }
        }

        public override void VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitType( node );

#if ROSLYN_5_11_0_OR_GREATER
        // Roslyn routes a union declaration to its own visit method, which a syntax kind cannot override, so the
        // union needs an override of its own. The method is named only in the Roslyn variant that declares it, for
        // the reason explained in section 6 of Metalama.Framework/docs/2027.0/DECISIONS.md. Sharing the struct helper
        // is safe here, because the helper does not read the parameter list of the declaration, which holds the case
        // types of a union and not primary constructor parameters.
        public override void VisitUnionDeclaration( UnionDeclarationSyntax node ) => this.VisitType( node );
#endif

        public override void VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitEnumDeclaration( EnumDeclarationSyntax node ) => this.VisitType( node );

        public override void VisitDelegateDeclaration( DelegateDeclarationSyntax node ) => this.VisitType( node );
    }
}