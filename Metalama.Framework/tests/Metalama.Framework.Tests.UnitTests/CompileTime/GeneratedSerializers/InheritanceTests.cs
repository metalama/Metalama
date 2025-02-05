// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public sealed class InheritanceTests : SerializerTestBase
    {
        [Fact]
        public void BaseSerializerInTheSameAssembly()
        {
            // Verifies that when serializable base and derived type are defined in the same assembly, both get a generated serializer that serializes and deserializes.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int BaseField;
}
public class B : A
{
    public int DerivedField;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var typeA = project.GetType( "A" );
            var typeB = project.GetType( "B" );
            var serializerA = GetSerializer( typeA );
            var serializerB = GetSerializer( typeB );

            dynamic instanceA = Activator.CreateInstance( typeA )!;
            instanceA.BaseField = 13;

            dynamic instanceB = Activator.CreateInstance( typeB )!;
            instanceB.BaseField = 13;
            instanceB.DerivedField = 42;

            var constructorArgumentsWriterA = new TestArgumentsWriter();
            var initializationArgumentsWriterA = new TestArgumentsWriter();
            serializerA.SerializeObject( instanceA, constructorArgumentsWriterA, initializationArgumentsWriterA );

            var constructorArgumentsReaderA = constructorArgumentsWriterA.ToReader();
            var initializationArgumentsReaderA = initializationArgumentsWriterA.ToReader();

            dynamic deserializedA = serializerA.CreateInstance( typeA, constructorArgumentsReaderA );
            serializerA.DeserializeFields( ref deserializedA, initializationArgumentsReaderA );

            Assert.Equal( 13, deserializedA.BaseField );

            var constructorArgumentsWriterB = new TestArgumentsWriter();
            var initializationArgumentsWriterB = new TestArgumentsWriter();
            serializerB.SerializeObject( instanceB, constructorArgumentsWriterB, initializationArgumentsWriterB );

            var constructorArgumentsReaderB = constructorArgumentsWriterB.ToReader();
            var initializationArgumentsReaderB = initializationArgumentsWriterB.ToReader();

            dynamic deserializedB = serializerB.CreateInstance( typeB, constructorArgumentsReaderB );
            serializerB.DeserializeFields( ref deserializedB, initializationArgumentsReaderB );

            Assert.Equal( 13, deserializedB.BaseField );
            Assert.Equal( 42, deserializedB.DerivedField );
        }

        [Fact]
        public void AbstractBase()
        {
            // Verifies that a serializable type derived from abstract base can be serialized and deserialized
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public abstract class A : ICompileTimeSerializable
{
    public int PropertyA { get; }

    public A(int propertyA)
    {
        this.PropertyA = propertyA;
    }
}
public class B : A
{
    public int PropertyB { get; }

    public B(int propertyA, int propertyB) : base(propertyA)
    {
        this.PropertyB = propertyB;
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var typeA = project.GetType( "A" );
            var typeB = project.GetType( "B" );
            var lamaSerializerA = GetSerializer( typeA );
            var lamaSerializerB = GetSerializer( typeB );

            Assert.Throws<InvalidOperationException>( () => lamaSerializerA.CreateInstance( typeA, new TestArgumentsReader() ) );

            dynamic instance = Activator.CreateInstance( typeB, 13, 42 )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializerB.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializerB.CreateInstance( typeB, constructorArgumentsReader );
            lamaSerializerB.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.PropertyA );
            Assert.Equal( 42, deserialized.PropertyB );
        }

        [Fact]
        public void NewSlot()
        {
            // Verifies that a conflict in serializable field/property names does not break serialization.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Property { get; }

    public A(int property)
    {
        this.Property = property;
    }
}
public class B : A
{
    public new int Property { get; }

    public B(int propertyA, int propertyB) : base(propertyA)
    {
        this.Property = propertyB;
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var typeA = project.GetType( "A" );
            var typeB = project.GetType( "B" );
            var lamaSerializer = GetSerializer( typeB );

            dynamic instance = Activator.CreateInstance( typeB, 13, 42 )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( typeB, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, typeA.GetProperty( "Property" )!.GetValue( deserialized ) );
            Assert.Equal( 42, typeB.GetProperty( "Property" )!.GetValue( deserialized ) );
        }

        [Fact]
        public void GenericBase()
        {
            // Verifies that a type with generic base type gets a correct serializer generated.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public abstract class A<T> : ICompileTimeSerializable
{
    public int PropertyA { get; }

    public A(int propertyA)
    {
        this.PropertyA = propertyA;
    }
}
public class B<T> : A<T>
{
    public int PropertyB { get; }

    public B(int propertyA, int propertyB) : base(propertyA)
    {
        this.PropertyB = propertyB;
    }
}
public class C : A<int>
{
    public int PropertyC { get; }

    public C(int propertyA, int propertyC) : base(propertyA)
    {
        this.PropertyC = propertyC;
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var typeB = project.GetType( "B`1" ).MakeGenericType( typeof(int) );
            var typeC = project.GetType( "C" );
            var lamaSerializerB = GetSerializer( typeB );
            var lamaSerializerC = GetSerializer( typeC );

            dynamic instanceB = Activator.CreateInstance( typeB, 13, 42 )!;
            dynamic instanceC = Activator.CreateInstance( typeC, 13, 42 )!;

            var constructorArgumentsWriterB = new TestArgumentsWriter();
            var initializationArgumentsWriterB = new TestArgumentsWriter();
            lamaSerializerB.SerializeObject( instanceB, constructorArgumentsWriterB, initializationArgumentsWriterB );

            var constructorArgumentsWriterC = new TestArgumentsWriter();
            var initializationArgumentsWriterC = new TestArgumentsWriter();
            lamaSerializerC.SerializeObject( instanceC, constructorArgumentsWriterC, initializationArgumentsWriterC );

            var constructorArgumentsReaderB = constructorArgumentsWriterB.ToReader();
            var initializationArgumentsReaderB = initializationArgumentsWriterB.ToReader();

            var constructorArgumentsReaderC = constructorArgumentsWriterC.ToReader();
            var initializationArgumentsReaderC = initializationArgumentsWriterC.ToReader();

            dynamic deserializedB = lamaSerializerB.CreateInstance( typeB, constructorArgumentsReaderB );
            lamaSerializerB.DeserializeFields( ref deserializedB, initializationArgumentsReaderB );

            dynamic deserializedC = lamaSerializerC.CreateInstance( typeC, constructorArgumentsReaderC );
            lamaSerializerC.DeserializeFields( ref deserializedC, initializationArgumentsReaderC );

            Assert.Equal( 13, deserializedB.PropertyA );
            Assert.Equal( 42, deserializedB.PropertyB );
            Assert.Equal( 13, deserializedC.PropertyA );
            Assert.Equal( 42, deserializedC.PropertyC );
        }

        [Fact]
        public void NestedGenericDerived()
        {
            // Verifies that when a type is a nested generic type with the parent generic type as a base, the generated serializer is correct.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A<T> : ICompileTimeSerializable
{
    public T FieldA;
    public T PropertyA { get; set; }

    public class B<U> : A<T>
    {
        public T FieldB;
        public U PropertyB { get; set; }
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A`1+B`1" ).MakeGenericType( typeof(int), typeof(double) );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            instance.FieldA = 13;
            instance.PropertyA = 17;
            instance.FieldB = 27;
            instance.PropertyB = 42.0;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.FieldA );
            Assert.Equal( 17, deserialized.PropertyA );
            Assert.Equal( 27, deserialized.FieldB );
            Assert.Equal( 42.0, deserialized.PropertyB );
        }
    }
}