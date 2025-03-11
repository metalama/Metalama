// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Metrics;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Extensions.Metrics.UnitTests
{
    public class StatementNumberTests : UnitTestClass
    {
        [Fact]
        public void SimpleTest()
        {
            var services = CreateAdditionalServiceCollection( new StatementsCountMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            var code = @"
class C 
{
  void M1 () {}
  void M2()
  {
    var x = 0;
    x++; 
  }
}

";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();

            var m1 = type.Methods.OfName( "M1" ).Single();
            Assert.Equal( 0, m1.Metrics().Get<StatementsCount>().Value );

            var m2 = type.Methods.OfName( "M2" ).Single();
            Assert.Equal( 2, m2.Metrics().Get<StatementsCount>().Value );

            Assert.Equal( 2, type.Metrics().Get<StatementsCount>().Value );
        }
    }
}