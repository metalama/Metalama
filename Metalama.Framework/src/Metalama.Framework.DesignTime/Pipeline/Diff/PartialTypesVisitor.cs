// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

internal sealed class PartialTypesVisitor : CSharpSyntaxVisitor<ImmutableArray<BaseTypeDeclarationSyntax>>
{
    public static PartialTypesVisitor Instance { get; } = new();

    private PartialTypesVisitor() { }

    public override ImmutableArray<BaseTypeDeclarationSyntax> DefaultVisit( SyntaxNode node )
    {
        var partialTypes = ImmutableArray<BaseTypeDeclarationSyntax>.Empty;

        foreach ( var child in node.ChildNodesAndTokens() )
        {
            if ( child.IsNode )
            {
                var newPartialTypes = this.Visit( child.AsNode() );

                if ( !newPartialTypes.IsDefaultOrEmpty )
                {
                    partialTypes = partialTypes.AddRange( newPartialTypes );
                }
            }
        }

        return partialTypes;
    }

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitClassDeclaration( ClassDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitStructDeclaration( StructDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitRecordDeclaration( RecordDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );

#if ROSLYN_5_11_0_OR_GREATER
    // The base class of this visitor does not descend into the children of a node it does not know, so without this
    // override a partial union is never collected. The method is named only in the Roslyn variant that declares it,
    // for the reason explained in section 6 of Metalama.Framework/docs/2027.0/DECISIONS.md.
    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitUnionDeclaration( UnionDeclarationSyntax node ) => VisitBaseTypeDeclaration( node );
#endif

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitGlobalStatement( GlobalStatementSyntax node )
        => ImmutableArray<BaseTypeDeclarationSyntax>.Empty;

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitUsingDirective( UsingDirectiveSyntax node )
        => ImmutableArray<BaseTypeDeclarationSyntax>.Empty;

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitDelegateDeclaration( DelegateDeclarationSyntax node )
        => ImmutableArray<BaseTypeDeclarationSyntax>.Empty;

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitEnumDeclaration( EnumDeclarationSyntax node )
        => ImmutableArray<BaseTypeDeclarationSyntax>.Empty;

    public override ImmutableArray<BaseTypeDeclarationSyntax> VisitMethodDeclaration( MethodDeclarationSyntax node )
        => ImmutableArray<BaseTypeDeclarationSyntax>.Empty;

    private static ImmutableArray<BaseTypeDeclarationSyntax> VisitBaseTypeDeclaration( BaseTypeDeclarationSyntax type )
    {
        if ( type.Modifiers.Any( SyntaxKind.PartialKeyword ) )
        {
            return ImmutableArray.Create( type );
        }

        return ImmutableArray<BaseTypeDeclarationSyntax>.Empty;
    }
}