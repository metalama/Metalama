// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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