// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class MetalamaGenericsTests : ReflectionTestBase
    {
        public MetalamaGenericsTests( ITestOutputHelper helper ) : base( helper ) { }

        [Fact]
        public void FieldInNestedType()
        {
            const string code = "class Target<TKey> { class Nested<TValue> { public System.Collections.Generic.Dictionary<TKey,TValue> Field; } }";

            using var testContext = this.CreateSerializationTestContext( code );

            var serialized = testContext.Serialize(
                    CompileTimeFieldOrPropertyInfo.Create( testContext.Compilation.Types.Single().Types.Single().Fields.Single() ) )
                .ToString();

            this.AssertEqual(
                @"new global::Metalama.Framework.RunTime.FieldOrPropertyInfo(typeof(global::Target<>.Nested<>).GetField(""Field"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance)!)",
                serialized );

            this.TestExpression<FieldInfo>(
                code,
                MetalamaPropertyInfoTests.StripLocationInfo( serialized ),
                info =>
                {
                    Assert.Equal( "Field", info.Name );
                    Assert.Equal( "Dictionary`2", info.FieldType.Name );
                } );
        }

        [Fact]
        public void MethodInDerivedType()
        {
            const string code = "class Target<TKey> : Origin<int, TKey> { TKey ReturnSelf() { return default(TKey); } } class Origin<TA, TB> { }";

            using var testContext = this.CreateSerializationTestContext( code );

            var serialized = testContext.Serialize(
                    CompileTimeMethodInfo.Create( testContext.Compilation.Types.Single( t => t.Name == "Target" ).Methods.First() ) )
                .ToString();

            this.AssertEqual(
                @"((global::System.Reflection.MethodInfo)global::Metalama.Framework.RunTime.ReflectionHelper.GetMethod(typeof(global::Target<>), ""ReturnSelf"", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, ""TKey ReturnSelf()"")!)",
                serialized );

            this.TestExpression<MethodInfo>(
                code,
                serialized,
                info =>
                {
                    Assert.Equal( "ReturnSelf", info.Name );
                    Assert.Equal( "TKey", info.ReturnParameter.AssertNotNull().ParameterType.Name );
                } );
        }

        [Fact]
        public void TestDerivedGenericType()
        {
            const string code = "class Target<T1> { } class User<T2> : Target<T2> { }";

            using var testContext = this.CreateSerializationTestContext( code );

            var serialized = testContext
                .Serialize<Type>( CompileTimeType.Create( testContext.Compilation.Types.Single( t => t.Name == "User" ).BaseType! ) )
                .ToString();

            this.TestExpression<Type>( code, serialized, info => Assert.Equal( "Target`1[T2]", info.ToString() ) );

            var serialized2 = testContext
                .Serialize<Type>( CompileTimeType.Create( testContext.Compilation.Types.Single( t => t.Name == "Target" ) ) )
                .ToString();

            this.TestExpression<Type>( code, serialized2, info => Assert.Equal( "Target`1[T1]", info.ToString() ) );
        }

        [Fact]
        public void TestHalfTypes()
        {
            const string code = "class Target<T1, T2> { void Method(T1 a, T2 b) { } } class User<T> : Target<int, T> { }";

            using var testContext = this.CreateSerializationTestContext( code );

            var serialized = testContext.Serialize(
                    CompileTimeMethodInfo.Create( testContext.Compilation.Types.Single( t => t.Name == "User" ).BaseType!.Methods.First() ) )
                .ToString();

            this.TestExpression<MethodInfo>( code, serialized, info => Assert.Equal( "Target`2[System.Int32,T]", info.DeclaringType?.ToString() ) );
            const string code2 = "class Target<T1, T2> { void Method(T1 a, T2 b) { } } class User<T> : Target<T, int> { }";

            using var testContext2 = this.CreateSerializationTestContext( code2 );

            var serialized2 = testContext2.Serialize(
                    CompileTimeMethodInfo.Create( testContext2.Compilation.Types.Single( t => t.Name == "User" ).BaseType!.Methods.First() ) )
                .ToString();

            this.TestExpression<MethodInfo>( code2, serialized2, info => Assert.Equal( "Target`2[T,System.Int32]", info.DeclaringType?.ToString() ) );
        }
    }
}