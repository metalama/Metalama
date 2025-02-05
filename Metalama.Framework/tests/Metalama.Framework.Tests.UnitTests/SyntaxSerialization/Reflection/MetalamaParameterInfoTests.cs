// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CodeModel.Source.Pseudo;
using Metalama.Framework.Engine.ReflectionMocks;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaParameterInfoTests : ReflectionTestBase
    {
        public MetalamaParameterInfoTests( ITestOutputHelper helper ) : base( helper ) { }

        [Fact]
        public void TestParameter()
        {
            const string code = "class Target { public static int Method(int target) => 2*target; }";
            var serialized = this.SerializeParameter( code );

            this.AssertEqual(
                @"typeof(global::Target).GetMethod(""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, null, new[] { typeof(global::System.Int32) }, null)!.GetParameters()[0]",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( "target", parameterInfo.Name );
                    Assert.Equal( 0, parameterInfo.Position );
                    Assert.Equal( typeof(int), parameterInfo.ParameterType );
                } );
        }

        [Fact]
        public void TestGenericParameter_GenericInMethod()
        {
            const string code = "class Target { public static int Method<T>(T target) => 4; }";
            var serialized = this.SerializeParameter( code );

            this.AssertEqual(
                @"global::Metalama.Framework.RunTime.ReflectionHelper.GetMethod(typeof(global::Target), ""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, ""Int32 Method[T](T)"")!.GetParameters()[0]",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( "target", parameterInfo.Name );
                    Assert.Equal( 0, parameterInfo.Position );
                    Assert.Equal( "T", parameterInfo.ParameterType.Name );
                } );
        }

        [Fact]
        public void TestGenericParameter_GenericInTypeAndMethod()
        {
            const string code = "class Target<T> { public static int Method<U>(System.Tuple<T,U> target) => 4; }";
            var serialized = this.SerializeParameter( code );

            this.AssertEqual(
                @"global::Metalama.Framework.RunTime.ReflectionHelper.GetMethod(typeof(global::Target<>), ""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, ""Int32 Method[U](System.Tuple`2[T,U])"")!.GetParameters()[0]",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( "target", parameterInfo.Name );
                    Assert.Equal( 0, parameterInfo.Position );
                    Assert.Equal( "Tuple`2", parameterInfo.ParameterType.Name );
                } );
        }

        [Fact]
        public void TestParameterInSecondPlace()
        {
            const string code = "class Target { public static int Method(float ignored, int target) => 2*target; }";
            var serialized = this.SerializeParameter( code );

            this.AssertEqual(
                @"typeof(global::Target).GetMethod(""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, null, new[] { typeof(global::System.Single), typeof(global::System.Int32) }, null)!.GetParameters()[1]",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( "target", parameterInfo.Name );
                    Assert.Equal( 1, parameterInfo.Position );
                    Assert.Equal( typeof(int), parameterInfo.ParameterType );
                } );
        }

        [Fact]
        public void TestReturnParameter()
        {
            const string code = "class Target { public static string Method(float ignored, int target) => null; }";
            var serialized = this.SerializeReturnParameter( code );

            this.AssertEqual(
                @"typeof(global::Target).GetMethod(""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, null, new[] { typeof(global::System.Single), typeof(global::System.Int32) }, null)!.ReturnParameter",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( -1, parameterInfo.Position );
                    Assert.Equal( typeof(string), parameterInfo.ParameterType );
                } );
        }

        [Fact]
        public void TestReturnParameterOfProperty()
        {
            const string code = "class Target { public static string Property => null; }";
            var serialized = this.SerializeReturnParameterOfProperty( code );

            this.AssertEqual(
                @"typeof(global::Target).GetMethod(""get_Property"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static, null, global::System.Type.EmptyTypes, null)!.ReturnParameter",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( -1, parameterInfo.Position );
                    Assert.Equal( typeof(string), parameterInfo.ParameterType );
                } );
        }

        [Fact]
        public void TestParameterOfIndexer()
        {
            const string code = "class Target { public int this[int target] { get {return 0;} set{} }}";
            var serialized = this.SerializeIndexerParameter( code );

            this.AssertEqual(
                @"typeof(global::Target).GetProperty(""Item"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, typeof(global::System.Int32), new global::System.Type[] { typeof(global::System.Int32) }, null)!.GetIndexParameters()[0]",
                serialized );

            this.TestExpression<ParameterInfo>(
                code,
                serialized,
                parameterInfo =>
                {
                    Assert.Equal( "target", parameterInfo.Name );
                    Assert.Equal( 0, parameterInfo.Position );
                    Assert.Equal( typeof(int), parameterInfo.ParameterType );
                } );
        }

        private string SerializeIndexerParameter( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            var targetType = compilation.Types.Single( t => t.Name == "Target" );
            var single = targetType.Indexers.Single().Parameters.First( p => p.Name == "target" );
            var parameter = single;

            var actual =
                testContext.Serialize( CompileTimeParameterInfo.Create( parameter ) )
                    .ToString();

            return actual;
        }

        private string SerializeParameter( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;

            var single = compilation.Types.Single( t => t.Name == "Target" )
                .Methods.Single( m => m.Name == "Method" )
                .Parameters.First( p => p.Name == "target" );

            var parameter = (SourceParameter) single;

            var actual =
                testContext.Serialize( CompileTimeParameterInfo.Create( parameter ) )
                    .ToString();

            return actual;
        }

        private string SerializeReturnParameter( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            var single = compilation.Types.Single( t => t.Name == "Target" ).Methods.Single( m => m.Name == "Method" ).ReturnParameter;
            var p = (PseudoReturnParameter) single;

            var actual = testContext.Serialize( CompileTimeReturnParameterInfo.Create( p ) )
                .ToString();

            return actual;
        }

        private string SerializeReturnParameterOfProperty( string code )
        {
            using var testContext = this.CreateSerializationTestContext( code );

            var compilation = testContext.Compilation;
            var single = compilation.Types.Single( t => t.Name == "Target" ).Properties.Single( m => m.Name == "Property" ).GetMethod!.ReturnParameter;
            var p = (PseudoReturnParameter) single;

            var actual = testContext.Serialize( CompileTimeReturnParameterInfo.Create( p ) )
                .ToString();

            return actual;
        }
    }
}