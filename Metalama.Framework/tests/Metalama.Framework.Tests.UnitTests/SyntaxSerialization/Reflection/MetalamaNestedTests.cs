// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaNestedTests : ReflectionTestBase
    {
        [Fact]
        public void TestType()
        {
            const string code = "class Target { class Sub { }  }";
            var serialized = this.SerializeType( code );

            this.AssertEqual(
                @"typeof(global::Target.Sub)",
                serialized );

            this.TestExpression<Type>( code, serialized, info => Assert.Equal( "Sub", info.Name ) );
        }

        private string SerializeType( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            IType single = compilation.Types.Single( t => t.Name == "Target" ).Types.Single( nt => nt.Name == "Sub" );

            return testContext.Serialize<Type>( CompileTimeType.Create( single ) ).ToString();
        }

        public MetalamaNestedTests( ITestOutputHelper helper ) : base( helper ) { }
    }
}