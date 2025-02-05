// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerLinkingStep
{
    private sealed class CleanupRewriter : SafeSyntaxRewriter
    {
        private readonly IProjectOptions? _projectOptions;
        private readonly SyntaxGenerationContext _generationContext;

        public CleanupRewriter( IProjectOptions? projectOptions, SyntaxGenerationContext generationContext )
        {
            this._projectOptions = projectOptions;
            this._generationContext = generationContext;
        }

        public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
            => node
                .WithBody( this.RewriteBodyBlock( node.Body ) );

        public override SyntaxNode VisitOperatorDeclaration( OperatorDeclarationSyntax node )
            => node
                .WithBody( this.RewriteBodyBlock( node.Body ) );

        public override SyntaxNode VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node )
            => node
                .WithBody( this.RewriteBodyBlock( node.Body ) );

        public override SyntaxNode VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
            => node
                .WithBody( this.RewriteBodyBlock( node.Body ) );

        public override SyntaxNode VisitDestructorDeclaration( DestructorDeclarationSyntax node )
            => node
                .WithBody( this.RewriteBodyBlock( node.Body ) );

        public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node ) => this.VisitBasePropertyDeclaration( node );

        public override SyntaxNode VisitIndexerDeclaration( IndexerDeclarationSyntax node ) => this.VisitBasePropertyDeclaration( node );

        public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node ) => this.VisitBasePropertyDeclaration( node );

        private SyntaxNode VisitBasePropertyDeclaration( BasePropertyDeclarationSyntax node )
            => node.WithAccessorList(
                node.AccessorList?.WithAccessors(
                    List(
                        node.AccessorList.Accessors
                            .SelectAsReadOnlyList( a => a.WithBody( this.RewriteBodyBlock( a.Body ) ) ) ) ) );

        private BlockSyntax? RewriteBodyBlock( BlockSyntax? block )
        {
            if ( block == null )
            {
                return null;
            }
            else if ( this._projectOptions?.CodeFormattingOptions == CodeFormattingOptions.Formatted )
            {
                var countLabelUsesWalker = new CountLabelUsesWalker();
                countLabelUsesWalker.Visit( block );

                var withFlattenedBlocks = new CleanupBodyRewriter( this._generationContext ).Visit( block );

                var withoutTrivialLabels =
                    new RemoveTrivialLabelRewriter( countLabelUsesWalker.ObservedLabelCounters, this._generationContext ).Visit( withFlattenedBlocks );

                var withoutTrailingReturns = new RemoveTrailingReturnRewriter( this._generationContext ).Visit( withoutTrivialLabels );

                return (BlockSyntax?) withoutTrailingReturns;
            }
            else
            {
                return (BlockSyntax?) new CleanupBodyRewriter( this._generationContext ).Visit( block );
            }
        }
    }
}