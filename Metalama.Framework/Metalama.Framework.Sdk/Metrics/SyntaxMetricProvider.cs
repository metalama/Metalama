// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Metrics
{
    /// <summary>
    /// An implementation of <see cref="IMetricProvider{T}"/> that is based on a <see cref="CSharpSyntaxVisitor"/>.
    /// </summary>
    /// <typeparam name="T">Type of the metric.</typeparam>
    public abstract class SyntaxMetricProvider<T> : MetricProvider<T>
        where T : struct, IMetric
    {
        private readonly BaseVisitor _visitor;

        protected SyntaxMetricProvider( BaseVisitor visitor )
        {
            this._visitor = visitor;
            visitor.Parent = this;
        }

        protected sealed override T ComputeMetricForType( INamedType namedType ) => this.Compute( namedType );

        protected sealed override T ComputeMetricForMember( IMember member ) => this.Compute( member );

        private T Compute( IDeclaration declaration )
        {
            var symbol = declaration.GetSymbol();

            if ( symbol == null )
            {
                // Not source code.
                return default;
            }

            var aggregate = default(T);

            foreach ( var syntaxRef in symbol.DeclaringSyntaxReferences )
            {
                var newValue = this._visitor.Visit( syntaxRef.GetSyntax() );
                this.Aggregate( ref aggregate, newValue );
            }

            return aggregate;
        }

        protected abstract class BaseVisitor : CSharpSyntaxVisitor<T>
        {
            private SyntaxMetricProvider<T>? _parent;

            internal SyntaxMetricProvider<T> Parent
            {
                get => this._parent ?? throw new InvalidOperationException();
                set
                {
                    if ( this._parent != null )
                    {
                        throw new InvalidOperationException();
                    }

                    this._parent = value;
                }
            }

            public override T DefaultVisit( SyntaxNode node )
            {
                var aggregate = default(T);

                foreach ( var nodeOrToken in node.ChildNodesAndTokens() )
                {
                    if ( nodeOrToken.IsNode )
                    {
                        var nodeResult = this.Visit( nodeOrToken.AsNode() );
                        this.Parent.Aggregate( ref aggregate, nodeResult );
                    }
                }

                return aggregate;
            }
        }
    }
}