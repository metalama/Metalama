// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.SyntaxBuilders;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating
{
    public sealed class InterpolatedStringBuilderTests
    {
        [Fact]
        public void TestClone()
        {
            var builder = new InterpolatedStringBuilder();

            // Note that production code always sends an expression (not its value) to the builder.
            builder.AddText( "1" );
            builder.AddExpression( 2 );

            var clone = builder.Clone();

            Assert.NotSame( clone, builder );
            Assert.Equal( builder.Items, clone.Items );

            builder.AddText( "4" );
            Assert.NotEqual( builder.Items, clone.Items );
        }
    }
}