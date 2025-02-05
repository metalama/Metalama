// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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