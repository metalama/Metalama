// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaTypeTests : ReflectionTestBase
    {
        [Fact]
        public void TestType()
        {
            const string code = "class Target {  }";
            var serialized = this.SerializeType( code );
            this.AssertEqual( @"typeof(global::Target)", serialized );

            this.TestExpression<Type>( code, serialized, info => Assert.Equal( "Target", info.Name ) );
        }

        [Fact]
        public void TestGenericType()
        {
            const string code = "class Target<TKey,TValue> {  }";
            var serialized = this.SerializeType( code );
            this.AssertEqual( @"typeof(global::Target<,>)", serialized );

            this.TestExpression<Type>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "Target`2", info.Name );
                    Assert.Equal( 2, info.GetGenericArguments().Length );
                } );
        }

        [Fact]
        public void TestArrayType()
        {
            const string code = "class Target { int[] Property { get; set; } }";
            var serialized = this.SerializeTypeOfProperty( code );

            this.AssertEqual(
                @"typeof(global::System.Int32[])",
                serialized );

            this.TestExpression<Type>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "System.Int32[]", info.FullName );
                    Assert.Equal( typeof(int[]), info );
                } );
        }

        [Fact]
        public void TestMultidimensionalArrayType()
        {
            const string code = "class Target { int[,] Property { get; set; } }";
            var serialized = this.SerializeTypeOfProperty( code );

            this.AssertEqual(
                @"typeof(global::System.Int32[,])",
                serialized );

            this.TestExpression<Type>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "System.Int32[,]", info.FullName );
                    Assert.Equal( typeof(int[,]), info );
                } );
        }

        // Types other than named types and array types are not implemented.

        private string SerializeType( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            IType single = compilation.Types.Single( t => t.Name == "Target" );
            var actual = testContext.Serialize<Type>( CompileTimeType.Create( single ) ).ToString();

            return actual;
        }

        private string SerializeTypeOfProperty( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            var single = compilation.Types.Single( t => t.Name == "Target" ).Properties.Single( p => p.Name == "Property" ).Type;
            var actual = testContext.Serialize<Type>( CompileTimeType.Create( single ) ).ToString();

            return actual;
        }

        public MetalamaTypeTests( ITestOutputHelper helper ) : base( helper ) { }
    }
}