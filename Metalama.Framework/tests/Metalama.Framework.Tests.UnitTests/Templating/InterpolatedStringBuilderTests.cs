// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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