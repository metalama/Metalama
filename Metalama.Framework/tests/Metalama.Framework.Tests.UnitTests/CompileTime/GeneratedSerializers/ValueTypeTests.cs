// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public sealed class ValueTypeTests : SerializerTestBase
    {
        [Fact]
        public void SimpleStruct()
        {
            // Verifies that serializable struct type can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public struct A : ICompileTimeSerializable
{
    public readonly int Field;
    public int Property { get; }
    public int MutableProperty { get; set; }

    public A(int field, int property)
    {
        this.Field = field;
        this.Property = property;
        this.MutableProperty = 0;
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type, 13, 27 )!;
            instance.MutableProperty = 42;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.Field );
            Assert.Equal( 27, deserialized.Property );
            Assert.Equal( 42, deserialized.MutableProperty );
        }

        [Fact]
        public void ReadonlyStruct()
        {
            // Verifies that serializable readonly struct type can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public readonly struct A : ICompileTimeSerializable
{
    public readonly int Field;
    public int Property { get; }

    public A(int field, int property)
    {
        this.Field = field;
        this.Property = property;
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type, 13, 42 )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.Field );
            Assert.Equal( 42, deserialized.Property );
        }
    }
}