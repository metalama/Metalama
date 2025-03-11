// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.ReflectionMocks;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaStateMachineTests : ReflectionTestBase
    {
        public MetalamaStateMachineTests( ITestOutputHelper helper ) : base( helper ) { }

        [Fact]
        public void TestEnumerable()
        {
            const string code = "class Target { public static System.Collections.Generic.IEnumerable<int> Method() { yield return 2; } }";
            using var testContext = this.CreateSerializationTestContext( code );

            var serialized = testContext.Serialize(
                    CompileTimeMethodInfo.Create( testContext.Compilation.Types.Single( t => t.Name == "Target" ).Methods.First() ) )
                .ToString();

            this.AssertEqual(
                @"((global::System.Reflection.MethodInfo)typeof(global::Target).GetMethod(""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, null, global::System.Type.EmptyTypes, null)!)",
                serialized );

            this.TestExpression<MethodInfo>( code, serialized, info => Assert.Equal( "Method", info.Name ) );
        }

        [Fact]
        public void Test()
        {
            const string code = "class Target { public static async void Method() { await System.Threading.Tasks.Task.Delay(1); } }";

            using var testContext = this.CreateSerializationTestContext( code );

            var serialized = testContext.Serialize(
                    CompileTimeMethodInfo.Create( testContext.Compilation.Types.Single( t => t.Name == "Target" ).Methods.First() ) )
                .ToString();

            this.AssertEqual(
                @"((global::System.Reflection.MethodInfo)typeof(global::Target).GetMethod(""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, null, global::System.Type.EmptyTypes, null)!)",
                serialized );

            this.TestExpression<MethodInfo>( code, serialized, info => Assert.Equal( "Method", info.Name ) );
        }
    }
}