// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Testing.UnitTesting;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating
{
    public sealed class ArrayBuilderTests : UnitTestClass
    {
        [Fact]
        public void Clone()
        {
            using var testContext = this.CreateTestContext();

            var model = testContext.CreateCompilationModel( "" );
            var builder = new ArrayBuilder( model.Factory.GetSpecialType( SpecialType.Object ) );

            // Note that production code always sends an expression (not its value) to the builder.
            builder.Add( "1" );
            builder.Add( 2 );
            builder.Add( DateTime.Now );

            var clone = builder.Clone();

            Assert.NotSame( clone, builder );
            Assert.Equal( builder.Items, clone.Items );

            builder.Add( 4 );
            Assert.NotEqual( builder.Items, clone.Items );
        }

        [Fact]
        public void OutOfContext()
        {
            Assert.Throws<InvalidOperationException>( () => new ArrayBuilder( typeof(int) ) );
            Assert.Throws<InvalidOperationException>( () => new ArrayBuilder() );
        }
    }
}