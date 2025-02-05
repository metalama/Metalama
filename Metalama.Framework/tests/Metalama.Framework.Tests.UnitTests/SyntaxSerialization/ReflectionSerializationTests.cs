// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

            var methodInfo = (MethodInfo) this.ExecuteExpression( code, expression )!;

            Assert.NotNull( methodInfo );
        }

        [Fact]
        public void TestGenericMethod()
        {
            const string code = "class Target { public static T Method<T>(T a) => (T)(object)(2*(int)(object)a); }";

            const string serialized =
                "System.Reflection.MethodBase.GetMethodFromHandle(Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(\"M:Target.Method``1(``0)~``0\"))";

            var methodInfo = (MethodInfo) this.ExecuteExpression( code, serialized )!;
            Assert.Equal( 42, methodInfo.MakeGenericMethod( typeof(int) ).Invoke( null, new object[] { 21 } ) );
        }

        [Fact]
        public void TestFieldInGenericType()
        {
            const string code = "class Target<T> { int f; }";

            const string serialized = @"
System.Reflection.FieldInfo.GetFieldFromHandle(
    Metalama.Compiler.Intrinsics.GetRuntimeFieldHandle(""F:Target`1.f""),
    Metalama.Compiler.Intrinsics.GetRuntimeTypeHandle(""T:Target`1""))";

            var fieldInfo = (FieldInfo) this.ExecuteExpression( code, serialized )!;
            Assert.Equal( "f", fieldInfo.Name );
        }

        [Fact]
        public void TestGenericType()
        {
            const string code = "class Target<T> { }";
            const string serialized = "System.Type.GetTypeFromHandle(Metalama.Compiler.Intrinsics.GetRuntimeTypeHandle(\"T:Target`1\"))";
            var type = (Type) this.ExecuteExpression( code, serialized )!;
            Assert.Equal( "Target`1", type.FullName );
        }
    }
}