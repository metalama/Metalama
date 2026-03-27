// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateExpansionContext
    {
        /// <summary>
        /// Visitor that detects yield statements inside try blocks that have catch clauses.
        /// C# does not allow yield return/break inside a try block with a catch clause,
        /// inside a catch block, or inside a finally block.
        /// </summary>
        private sealed class HasYieldInTryCatchVisitor : SafeSyntaxVisitor<bool>
        {
            private bool _insideTryWithCatch;

            public override bool DefaultVisit( SyntaxNode node )
            {
                foreach ( var child in node.ChildNodesAndTokens() )
                {
                    var childNode = child.AsNode();

                    if ( childNode != null && this.Visit( childNode ) )
                    {
                        return true;
                    }
                }

                return false;
            }

            public override bool VisitTryStatement( TryStatementSyntax node )
            {
                if ( node.Catches.Count > 0 )
                {
                    var previousValue = this._insideTryWithCatch;
                    this._insideTryWithCatch = true;

                    // Check the try block.
                    var result = this.Visit( node.Block );

                    // Also check catch blocks — yield is prohibited in catch blocks.
                    if ( !result )
                    {
                        foreach ( var catchClause in node.Catches )
                        {
                            if ( this.Visit( catchClause.Block ) )
                            {
                                result = true;

                                break;
                            }
                        }
                    }

                    // Also check finally block — yield is prohibited in finally blocks.
                    if ( !result && node.Finally != null )
                    {
                        result = this.Visit( node.Finally.Block );
                    }

                    this._insideTryWithCatch = previousValue;

                    return result;
                }

                // try-finally without catch is fine for yield.
                return this.DefaultVisit( node );
            }

            public override bool VisitYieldStatement( YieldStatementSyntax node ) => this._insideTryWithCatch;

            // Don't descend into local functions or lambdas — they have their own scope for yield.
            public override bool VisitLocalFunctionStatement( LocalFunctionStatementSyntax node ) => false;

            public override bool VisitParenthesizedLambdaExpression( ParenthesizedLambdaExpressionSyntax node ) => false;

            public override bool VisitSimpleLambdaExpression( SimpleLambdaExpressionSyntax node ) => false;

            public override bool VisitAnonymousMethodExpression( AnonymousMethodExpressionSyntax node ) => false;
        }
    }
}
