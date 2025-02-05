// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CodeModel.Helpers
{
    internal static partial class IteratorHelper
    {
        /// <summary>
        /// Finds a 'yield' statement in the body.
        /// </summary>
        private sealed class FindYieldVisitor : SafeSyntaxVisitor<bool>
        {
            public static readonly FindYieldVisitor Instance = new();

            private FindYieldVisitor() { }

            public override bool VisitYieldStatement( YieldStatementSyntax node ) => true;

            public override bool DefaultVisit( SyntaxNode node )
            {
                switch ( node )
                {
                    case ExpressionSyntax:
                    case LocalFunctionStatementSyntax:
                        return false;

                    default:
                        foreach ( var childNode in node.ChildNodes() )
                        {
                            if ( this.Visit( childNode ) )
                            {
                                return true;
                            }
                        }

                        return false;
                }
            }
        }
    }
}