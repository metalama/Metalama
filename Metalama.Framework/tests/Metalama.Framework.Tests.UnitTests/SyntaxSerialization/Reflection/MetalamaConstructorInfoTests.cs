// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaConstructorInfoTests : ReflectionTestBase
    {
        [Fact]
        public void TestConstructor()
        {
            const string code = "class Target { public Target(int hello) { } }";
            var serialized = this.SerializeConstructor( code );

            this.AssertEqual(
                @"((global::System.Reflection.ConstructorInfo)typeof(global::Target).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!)",
                serialized );

            this.TestExpression<ConstructorInfo>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "Target", info.DeclaringType!.Name );
                    Assert.Single( info.GetParameters() );
                } );
        }

        [Fact]
        public void TestGenericConstructor()
        {
            const string code = "class Target<T> where T: struct { public Target(T hello) { } }";
            var serialized = this.SerializeConstructor( code );

            this.AssertEqual(
                @"((global::System.Reflection.ConstructorInfo)global::Metalama.Framework.RunTime.ReflectionHelper.GetConstructor(typeof(global::Target<>), global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, ""Void .ctor(T)"")!)",
                serialized );

            this.TestExpression<ConstructorInfo>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "Target`1", info.DeclaringType!.Name );
                    Assert.Single( info.GetParameters() );
                } );
        }

        [Fact]
        public void TestDefaultConstructor()
        {
            const string code = "class Target {  }";
            var serialized = this.SerializeConstructor( code );

            this.AssertEqual(
                @"((global::System.Reflection.ConstructorInfo)typeof(global::Target).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, global::System.Type.EmptyTypes, null)!)",
                serialized );

            this.TestExpression<ConstructorInfo>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "Target", info.DeclaringType!.Name );
                    Assert.Empty( info.GetParameters() );
                } );
        }

        // If there is no constructor, there is no constructor to serialize. We are at C#, not IL level.
        private string SerializeConstructor( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            var namedTypes = compilation.Types;
            var type = namedTypes.Single( t => t.Name == "Target" );
            var methods = type.Constructors;
            var single = methods.Single();
            var p = (single as SourceConstructor)!;
            var actual = testContext.Serialize( CompileTimeConstructorInfo.Create( p ) ).ToString();

            return actual;
        }

        public MetalamaConstructorInfoTests( ITestOutputHelper helper ) : base( helper ) { }
    }
}