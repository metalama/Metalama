// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class ReflectionSerializationTests : SerializerTestsBase
    {
        [Fact]
        public void MethodHandleTest()
        {
            const string code = @"
class C
{
    void M() {}
}";

            const string expression = "System.Reflection.MethodBase.GetMethodFromHandle(Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(\"M:C.M\"))";

            var methodInfo = this.ExecuteExpression<MethodInfo>( code, expression )!;

            Assert.NotNull( methodInfo );
        }

        [Fact]
        public void TestGenericMethod()
        {
            const string code = "class Target { public static T Method<T>(T a) => (T)(object)(2*(int)(object)a); }";

            const string serialized =
                "System.Reflection.MethodBase.GetMethodFromHandle(Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(\"M:Target.Method``1(``0)~``0\"))";

            var methodInfo = this.ExecuteExpression<MethodInfo>( code, serialized )!;
            Assert.Equal( 42, methodInfo.MakeGenericMethod( typeof(int) ).Invoke( null, [21] ) );
        }

        [Fact]
        public void TestFieldInGenericType()
        {
            const string code = "class Target<T> { int f; }";

            const string serialized = @"
System.Reflection.FieldInfo.GetFieldFromHandle(
    Metalama.Compiler.Intrinsics.GetRuntimeFieldHandle(""F:Target`1.f""),
    Metalama.Compiler.Intrinsics.GetRuntimeTypeHandle(""T:Target`1""))";

            var fieldInfo = this.ExecuteExpression<FieldInfo>( code, serialized )!;
            Assert.Equal( "f", fieldInfo.Name );
        }

        [Fact]
        public void TestGenericType()
        {
            const string code = "class Target<T> { }";
            const string serialized = "System.Type.GetTypeFromHandle(Metalama.Compiler.Intrinsics.GetRuntimeTypeHandle(\"T:Target`1\"))";
            var type = this.ExecuteExpression<Type>( code, serialized )!;
            Assert.Equal( "Target`1", type.FullName );
        }
    }
}