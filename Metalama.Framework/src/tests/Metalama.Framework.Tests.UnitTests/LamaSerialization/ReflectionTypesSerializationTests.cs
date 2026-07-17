// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Global
// Resharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class ReflectionTypesSerializationTests : SerializationTestsBase
    {
        [Fact]
        public void TestTypeClass()
        {
            this.TestSerialization( typeof(DateTime) );
            this.TestSerialization( typeof(Guid) );
            this.TestSerialization( typeof(IntrinsicSerializationTests) );
        }

        [Fact]
        public void TestTypeGenericClosed()
        {
            this.TestSerialization( typeof(Dictionary<string, string>) );
        }

        [Fact]
        public void TestTypeGenericOpen()
        {
            this.TestSerialization( typeof(Dictionary<,>) );
        }

        [Fact]
        public void TestTypeGenericTypeParameter()
        {
            this.TestSerialization( typeof(Dictionary<,>).GetGenericArguments()[0] );
        }

#pragma warning disable SA1401 // Fields should be private
        [UsedImplicitly]
        public int TestField;
#pragma warning restore SA1401 // Fields should be private

        [UsedImplicitly]
        public int TestProperty { get; set; }

        // TODO: Other, more esoteric reflection objects: generic parameters, method arguments etc.

        [Fact]
        public void TestTypeIntrinsics()
        {
            this.TestSerialization( typeof(byte) );
            this.TestSerialization( typeof(sbyte) );
            this.TestSerialization( typeof(short) );
            this.TestSerialization( typeof(ushort) );
            this.TestSerialization( typeof(int) );
            this.TestSerialization( typeof(uint) );
            this.TestSerialization( typeof(long) );
            this.TestSerialization( typeof(ulong) );
            this.TestSerialization( typeof(float) );
            this.TestSerialization( typeof(double) );
            this.TestSerialization( typeof(string) );
            this.TestSerialization( typeof(DottedString) );
            this.TestSerialization( typeof(char) );
            this.TestSerialization( typeof(object) );
            this.TestSerialization( typeof(void) );
            this.TestSerialization( typeof(Type) );
            this.TestSerialization( typeof(ValueType) );
        }

        // These represent System.Type values whose type cannot be bound to a real, loadable Type in the reading process
        // (here, because the type only exists in the ad-hoc test compilation, never emitted or loaded as an assembly) -
        // the same situation as a run-time type of the writing process that is incompatible with the reading process
        // (e.g. across TFMs or assembly versions). Deserialization must fall back to a symbolic CompileTimeType instead
        // of throwing or resolving to the wrong type.
        [Fact]
        public void TestTypeValue_UnresolvableSimpleType()
        {
            using var testContext = this.CreateTestContextWithCode( "public class MyRuntimeType { }" );

            Type mockType = CompileTimeTypeTestHelper.Create( testContext.Compilation.Types.OfName( "MyRuntimeType" ).Single() );

            var deserialized = TestSerialization( testContext, mockType, testEquality: false );

            var deserializedMock = Assert.IsAssignableFrom<CompileTimeType>( deserialized );
            Assert.Equal( mockType.Namespace, deserializedMock.Namespace );
            Assert.Equal( mockType.Name, deserializedMock.Name );
            Assert.Equal( mockType.FullName, deserializedMock.FullName );
        }

        [Fact]
        public void TestTypeValue_UnresolvableGenericType()
        {
            using var testContext = this.CreateTestContextWithCode(
                "public class MyGenericRuntimeType<T> { } "
                + "public class MyRuntimeHolder { public MyGenericRuntimeType<int> ClosedGenericField; }" );

            var holderType = testContext.Compilation.Types.OfName( "MyRuntimeHolder" ).Single();
            var fieldType = holderType.Fields.OfName( "ClosedGenericField" ).Single().Type;
            Type mockType = CompileTimeTypeTestHelper.Create( fieldType );

            var deserialized = TestSerialization( testContext, mockType, testEquality: false );

            var deserializedMock = Assert.IsAssignableFrom<CompileTimeType>( deserialized );
            Assert.Equal( mockType.FullName, deserializedMock.FullName );
        }

        [Fact]
        public void TestTypeValue_UnresolvableArrayType()
        {
            using var testContext = this.CreateTestContextWithCode(
                "public class MyRuntimeType { } "
                + "public class MyRuntimeHolder { public MyRuntimeType[] ArrayField; }" );

            var holderType = testContext.Compilation.Types.OfName( "MyRuntimeHolder" ).Single();
            var fieldType = holderType.Fields.OfName( "ArrayField" ).Single().Type;
            Type mockType = CompileTimeTypeTestHelper.Create( fieldType );

            var deserialized = TestSerialization( testContext, mockType, testEquality: false );

            var deserializedMock = Assert.IsAssignableFrom<CompileTimeType>( deserialized );
            Assert.Equal( mockType.FullName, deserializedMock.FullName );
        }

        [UsedImplicitly]
        public class ReflectionTestClass
        {
            public bool MethodInvoked { get; set; }

            public void Method()
            {
                this.MethodInvoked = true;
            }

#pragma warning disable CA1822 // Mark members as static
            public void MethodWithParameter( int parameter )
#pragma warning restore CA1822 // Mark members as static
            { }
        }
    }
}