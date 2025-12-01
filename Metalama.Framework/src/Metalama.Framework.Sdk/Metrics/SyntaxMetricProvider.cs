// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Metrics
{
    /// <summary>
    /// Base class for implementing custom metrics that analyze Roslyn syntax trees.
    /// Derive from this class when your metric needs to examine the actual code structure (statements, expressions, etc.).
    /// </summary>
    /// <typeparam name="T">The metric type, which must be a struct implementing <see cref="IMetric"/>.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="SyntaxMetricProvider{T}"/> simplifies syntax-based metric implementation by:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Automatically locating syntax nodes from declarations using <see cref="ISymbol.DeclaringSyntaxReferences"/>.</description></item>
    /// <item><description>Providing a <see cref="BaseVisitor"/> class that handles recursive aggregation by default.</description></item>
    /// <item><description>Handling partial declarations by visiting and aggregating each part.</description></item>
    /// </list>
    /// <para>
    /// To create a syntax-based metric:
    /// </para>
    /// <list type="number">
    /// <item><description>Create a metric struct implementing <see cref="IMetric{TTarget}"/>.</description></item>
    /// <item><description>Create a nested visitor class deriving from <see cref="BaseVisitor"/> and override <see cref="CSharpSyntaxVisitor{TResult}.Visit"/> methods.</description></item>
    /// <item><description>Create a provider class deriving from <see cref="SyntaxMetricProvider{T}"/>, passing the visitor to the constructor.</description></item>
    /// <item><description>Override <see cref="MetricProvider{T}.Aggregate"/> to combine values.</description></item>
    /// <item><description>Annotate the provider class with <see cref="Metalama.Framework.Engine.MetalamaPlugInAttribute"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MetricProvider{T}"/>
    /// <seealso cref="BaseVisitor"/>
    /// <seealso href="@custom-metrics"/>
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