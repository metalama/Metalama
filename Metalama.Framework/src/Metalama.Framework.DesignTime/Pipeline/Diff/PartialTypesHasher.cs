// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Discovers partial types in a syntax tree. The code is optimized to allocate a minimum of memory, ideally
/// zero when there is no partial type.
/// </summary>
internal sealed class PartialTypesHasher : CSharpSyntaxVisitor<int?>
{
    public static PartialTypesHasher Instance { get; } = new();

    private PartialTypesHasher() { }

    public override int? DefaultVisit( SyntaxNode node )
    {
        var combinedHash = 0;
        var hasAnyPartialType = false;

        foreach ( var child in node.ChildNodesAndTokens() )
        {
            if ( child.IsNode )
            {
                var childHash = this.Visit( child.AsNode() );

                if ( childHash.HasValue )
                {
                    combinedHash = HashCode.Combine( combinedHash, childHash.Value );
                    hasAnyPartialType = true;
                }
            }
        }

        return hasAnyPartialType ? combinedHash : null;
    }

    public override int? VisitClassDeclaration( ClassDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );

    public override int? VisitStructDeclaration( StructDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );

    public override int? VisitRecordDeclaration( RecordDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );

#if ROSLYN_5_10_0_OR_GREATER && ALLOW_PREVIEW_LANG_VERSION
    // The base class of this visitor does not descend into the children of a node it does not know, so without this
    // override a union declaration produces no hash at all. The method is named only in the Roslyn variant that
    // declares it, for the reason explained in section 6 of Metalama.Framework/docs/2027.0/DECISIONS.md.
    public override int? VisitUnionDeclaration( UnionDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );
#endif

    public override int? VisitGlobalStatement( GlobalStatementSyntax node ) => null;

    public override int? VisitUsingDirective( UsingDirectiveSyntax node ) => null;

    public override int? VisitDelegateDeclaration( DelegateDeclarationSyntax node ) => null;

    public override int? VisitEnumDeclaration( EnumDeclarationSyntax node ) => null;

    public override int? VisitMethodDeclaration( MethodDeclarationSyntax node ) => null;

    private static int? VisitBaseTypeDeclaration( BaseTypeDeclarationSyntax type )
    {
        if ( type.Modifiers.Any( SyntaxKind.PartialKeyword ) )
        {
            return type.Identifier.GetHashCode();
        }

        return null;
    }
}