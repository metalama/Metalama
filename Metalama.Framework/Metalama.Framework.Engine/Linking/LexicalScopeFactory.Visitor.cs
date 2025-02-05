// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LexicalScopeFactory
{
    private sealed class Visitor : SafeSyntaxWalker
    {
        private readonly ImmutableHashSet<string>.Builder _builder;

        public Visitor( ImmutableHashSet<string>.Builder builder )
        {
            this._builder = builder;
        }

        private void AddIdentifier( SyntaxToken identifierToken )
        {
            Invariant.Assert( identifierToken.IsKind( SyntaxKind.IdentifierToken ) );

            this._builder.Add( identifierToken.ValueText );
        }

        public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitLocalFunctionStatement( node );
        }

        public override void VisitVariableDeclarator( VariableDeclaratorSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitVariableDeclarator( node );
        }

        public override void VisitParameter( ParameterSyntax node ) => this.AddIdentifier( node.Identifier );

        public override void VisitTypeParameter( TypeParameterSyntax node ) => this.AddIdentifier( node.Identifier );

        public override void VisitSingleVariableDesignation( SingleVariableDesignationSyntax node ) => this.AddIdentifier( node.Identifier );

        public override void VisitFromClause( FromClauseSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitFromClause( node );
        }

        public override void VisitQueryContinuation( QueryContinuationSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitQueryContinuation( node );
        }

        public override void VisitLetClause( LetClauseSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitLetClause( node );
        }

        public override void VisitJoinClause( JoinClauseSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitJoinClause( node );
        }

        public override void VisitJoinIntoClause( JoinIntoClauseSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitJoinIntoClause( node );
        }

        public override void VisitLabeledStatement( LabeledStatementSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitLabeledStatement( node );
        }

        public override void VisitForEachStatement( ForEachStatementSyntax node )
        {
            this.AddIdentifier( node.Identifier );

            base.VisitForEachStatement( node );
        }

        public override void VisitCatchDeclaration( CatchDeclarationSyntax node )
        {
            if ( node.Identifier.IsKind( SyntaxKind.IdentifierToken ) )
            {
                this.AddIdentifier( node.Identifier );
            }

            base.VisitCatchDeclaration( node );
        }
    }
}