// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Metrics;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Extensions.Metrics.UnitTests
{
    /// <summary>
    /// Tests that record how the metric providers count a labeled loop, a labeled <c>break</c> and a labeled
    /// <c>continue</c>.
    /// </summary>
    /// <remarks>
    /// The two providers need no case of their own for these constructs. The statements count visits the statement
    /// nested within a labeled statement and adds nothing for the label, and the syntax nodes count visits every child
    /// node, so the label of a jump adds the one identifier node that carries it. See issue #1947.
    /// </remarks>
    public sealed class LabeledJumpTests : UnitTestClass
    {
        private const string _codeWithoutLabels = @"
class C
{
  int M()
  {
    var total = 0;

    for (var i = 0; i < 3; i++)
    {
      for (var j = 0; j < 3; j++)
      {
        if (j == 1) { continue; }

        total += i + j;

        if (total > 4) { break; }
      }
    }

    return total;
  }
}
";

        private const string _codeWithLabels = @"
class C
{
  int M()
  {
    var total = 0;

    outer:

    for (var i = 0; i < 3; i++)
    {
      for (var j = 0; j < 3; j++)
      {
        if (j == 1) { continue outer; }

        total += i + j;

        if (total > 4) { break outer; }
      }
    }

    return total;
  }
}
";

        [Fact]
        public void StatementsCountIsUnchangedByTheLabels()
        {
            Assert.Equal( this.GetStatementsCount( _codeWithoutLabels ), this.GetStatementsCount( _codeWithLabels ) );
        }

        [Fact]
        public void SyntaxNodesCountGrowsByTheLabeledStatementAndTheTwoNames()
        {
            // The labeled statement that carries the loop is one node, and the name of the break and the name of the
            // continue are one identifier node each.
            Assert.Equal(
                this.GetSyntaxNodesCount( _codeWithoutLabels ) + 3,
                this.GetSyntaxNodesCount( _codeWithLabels ) );
        }

        private int GetStatementsCount( string code )
        {
            var services = CreateAdditionalServiceCollection( new StatementsCountMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            var compilation = testContext.CreateCompilation( code );

            return compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single().Metrics().Get<StatementsCount>().Value;
        }

        private int GetSyntaxNodesCount( string code )
        {
            var services = CreateAdditionalServiceCollection( new SyntaxNodesCountMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            var compilation = testContext.CreateCompilation( code );

            return compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single().Metrics().Get<SyntaxNodesCount>().Value;
        }
    }
}
