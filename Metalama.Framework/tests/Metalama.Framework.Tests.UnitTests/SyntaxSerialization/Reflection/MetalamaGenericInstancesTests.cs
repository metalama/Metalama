// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaGenericInstancesTests : ReflectionTestBase
    {
        public MetalamaGenericInstancesTests( ITestOutputHelper helper ) : base( helper ) { }

        [Fact]
        public void TestListString()
        {
            this.AssertFieldType(
                "class Outer { class Inner { System.Collections.Generic.List<string> Target; } }",
                typeof(List<string>),
                @"typeof(global::System.Collections.Generic.List<global::System.String>)" );
        }

        [Fact]
        public void TestDictionaryString()
        {
            this.AssertFieldType(
                "class Outer { class Inner { System.Collections.Generic.Dictionary<string[],int?> Target; } }",
                typeof(Dictionary<string[], int?>),
                @"typeof(global::System.Collections.Generic.Dictionary<global::System.String[],global::System.Int32?>)" );
        }

        private void AssertFieldType( string code, Type expectedType, string expected )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var allTypes = testContext.Compilation.Types;
            var nestedTypes = allTypes.Single().Types;
            var innerType = nestedTypes.Single();
            var allProperties = innerType.Fields;

            var serialized = testContext.Serialize<Type>( CompileTimeType.Create( allProperties.Single().Type ) )
                .ToString();

            this.TestExpression<Type>( code, serialized, info => Assert.Equal( expectedType, info ) );

            this.AssertEqual( expected, serialized );
        }
    }
}