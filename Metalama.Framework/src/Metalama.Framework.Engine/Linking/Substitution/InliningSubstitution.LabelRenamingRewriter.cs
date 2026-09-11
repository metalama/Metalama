// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking.Substitution
{
    internal sealed partial class InliningSubstitution
    {
        /// <summary>
        /// Renames the labels of an inlined body and rewrites every statement that names one of them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The rewrite covers the three places where a label name appears: the identifier of a labeled statement, the
        /// target of a <c>goto</c> statement, and the name of a <c>break</c> or of a <c>continue</c> statement. The
        /// last two are a C# 15 feature, so the property that carries the name is declared by the latest Roslyn
        /// variant only and the two overrides that read it are compiled by that variant only.
        /// </para>
        /// <para>
        /// The rewrite replaces identifiers and never inserts a statement, so it cannot separate a label from the
        /// statement it labels. Only the statement immediately nested within a labeled statement carries that label.
        /// </para>
        /// </remarks>
        private sealed class LabelRenamingRewriter : SafeSyntaxRewriter
        {
            private readonly IReadOnlyDictionary<string, string> _renamedLabels;

            public LabelRenamingRewriter( IReadOnlyDictionary<string, string> renamedLabels )
            {
                this._renamedLabels = renamedLabels;
            }

            public override SyntaxNode? VisitLabeledStatement( LabeledStatementSyntax node )
            {
                var rewrittenNode = (LabeledStatementSyntax) base.VisitLabeledStatement( node ).AssertNotNull();

                if ( this._renamedLabels.TryGetValue( rewrittenNode.Identifier.ValueText, out var newName ) )
                {
                    return rewrittenNode.WithIdentifier(
                        SyntaxFactoryEx.SafeIdentifier(
                            rewrittenNode.Identifier.LeadingTrivia,
                            newName,
                            rewrittenNode.Identifier.TrailingTrivia ) );
                }

                return rewrittenNode;
            }

            public override SyntaxNode? VisitGotoStatement( GotoStatementSyntax node )
            {
                var rewrittenNode = (GotoStatementSyntax) base.VisitGotoStatement( node ).AssertNotNull();

                // A goto statement that names a label is of the GotoStatement kind. The GotoCaseStatement kind and the
                // GotoDefaultStatement kind name a section of a switch statement and not a label.
                if ( rewrittenNode.IsKind( SyntaxKind.GotoStatement )
                     && rewrittenNode.Expression is IdentifierNameSyntax identifierName
                     && this._renamedLabels.TryGetValue( identifierName.Identifier.ValueText, out var newName ) )
                {
                    return rewrittenNode.WithExpression( Rename( identifierName, newName ) );
                }

                return rewrittenNode;
            }

#if ROSLYN_5_11_0_OR_GREATER
            public override SyntaxNode? VisitBreakStatement( BreakStatementSyntax node )
            {
                var rewrittenNode = (BreakStatementSyntax) base.VisitBreakStatement( node ).AssertNotNull();

                if ( rewrittenNode.Name != null
                     && this._renamedLabels.TryGetValue( rewrittenNode.Name.Identifier.ValueText, out var newName ) )
                {
                    return rewrittenNode.WithName( Rename( rewrittenNode.Name, newName ) );
                }

                return rewrittenNode;
            }

            public override SyntaxNode? VisitContinueStatement( ContinueStatementSyntax node )
            {
                var rewrittenNode = (ContinueStatementSyntax) base.VisitContinueStatement( node ).AssertNotNull();

                if ( rewrittenNode.Name != null
                     && this._renamedLabels.TryGetValue( rewrittenNode.Name.Identifier.ValueText, out var newName ) )
                {
                    return rewrittenNode.WithName( Rename( rewrittenNode.Name, newName ) );
                }

                return rewrittenNode;
            }
#endif

            private static IdentifierNameSyntax Rename( IdentifierNameSyntax node, string newName )
                => SyntaxFactoryEx.SafeIdentifierName( node.Identifier.LeadingTrivia, newName, node.Identifier.TrailingTrivia );
        }
    }
}
