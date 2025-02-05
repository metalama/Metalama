// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Metrics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Metrics;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Xunit;

#pragma warning disable SA1402

namespace Metalama.Framework.Tests.UnitTests.Metrics
{
    public sealed class StatementCountMetricTests : UnitTestClass
    {
        [Fact]
        public void BasicTest()
        {
            var services = new AdditionalServiceCollection( new ForStatementNumberMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
  void M1()  { int k = 0; for ( int i = 0; i < 5; i++ ) {  k++; if ( k == 5 ) { k = 0; } for ( int j = 0; j < i; j++ ) { k++; }  } }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var m1 = compilation.Types.Single().Methods.OfName( "M1" ).Single();
            var metric = m1.Metrics().Get<ForStatementNumberMetric>();
            Assert.Equal( 2, metric.Count );
        }
    }

    internal struct ForStatementNumberMetric : IMetric<IMethod>, IMetric<INamedType>
    {
        public int Count { get; internal set; }
    }

    internal sealed class ForStatementNumberMetricProvider : SyntaxMetricProvider<ForStatementNumberMetric>
    {
        protected override void Aggregate( ref ForStatementNumberMetric aggregate, in ForStatementNumberMetric newValue ) => aggregate.Count += newValue.Count;

        private sealed class Visitor : BaseVisitor
        {
            public override ForStatementNumberMetric VisitForStatement( ForStatementSyntax node )
            {
                var aggregate = this.DefaultVisit( node );
                aggregate.Count++;

                return aggregate;
            }
        }

        public ForStatementNumberMetricProvider() : base( new Visitor() ) { }
    }
}