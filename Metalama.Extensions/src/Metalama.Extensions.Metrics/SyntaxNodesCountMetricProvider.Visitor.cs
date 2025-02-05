// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;

namespace Metalama.Extensions.Metrics
{
    public partial class SyntaxNodesCountMetricProvider
    {
        /// <summary>
        /// A visitor that counts the syntax nodes.
        /// </summary>
        [CompileTime]
        private class Visitor : BaseVisitor
        {
            public override SyntaxNodesCount DefaultVisit( SyntaxNode node )
            {
                var metric = new SyntaxNodesCount { Value = 1 };

                foreach ( var child in node.ChildNodes() )
                {
                    metric.Add( this.Visit( child ) );
                }

                return metric;
            }
        }
    }
}