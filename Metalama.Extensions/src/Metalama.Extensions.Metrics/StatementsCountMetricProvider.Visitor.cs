// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Extensions.Metrics
{
    public partial class StatementsCountMetricProvider
    {
        /// <summary>
        /// A visitor that counts the statements.
        /// </summary>
        [CompileTime]
        private class Visitor : BaseVisitor
        {
            public override StatementsCount DefaultVisit( SyntaxNode node )
            {
                var metric = default(StatementsCount);

                if ( node is StatementSyntax )
                {
                    metric.Value++;
                }

                foreach ( var child in node.ChildNodes() )
                {
                    metric.Add( this.Visit( child ) );
                }

                return metric;
            }

            public override StatementsCount VisitBlock( BlockSyntax node )
            {
                var metric = default(StatementsCount);

                foreach ( var statement in node.Statements )
                {
                    metric.Add( this.Visit( statement ) );
                }

                return metric;
            }

            public override StatementsCount VisitForStatement( ForStatementSyntax node )
            {
                // We intentionally ignore the assignment and increment statements because we don't want to count in trivial increments and initializations.

                var metric = this.Visit( node.Statement );

                metric.Value++;

                return metric;
            }

            public override StatementsCount VisitLabeledStatement( LabeledStatementSyntax node ) => this.Visit( node.Statement );

            public override StatementsCount VisitUnsafeStatement( UnsafeStatementSyntax node ) => this.Visit( node.Block );

            public override StatementsCount VisitTryStatement( TryStatementSyntax node )
            {
                var metric = default(StatementsCount);

                foreach ( var child in node.DescendantNodes() )
                {
                    metric.Add( this.Visit( child ) );
                }

                return metric;
            }
        }
    }
}