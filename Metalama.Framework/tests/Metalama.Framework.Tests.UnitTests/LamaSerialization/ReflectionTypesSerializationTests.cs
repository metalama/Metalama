// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime.Serialization;
using System;
using System.Collections.Generic;
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