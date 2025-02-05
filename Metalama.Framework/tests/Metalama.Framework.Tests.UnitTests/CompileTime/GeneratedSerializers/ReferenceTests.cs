// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public sealed class ReferenceTests : SerializerTestBase
    {
        [Fact]
        public void ReferenceType()
        {
            // Verifies that a reference-type property is deserialized in DeserializeFields method even if it was readonly.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public object? Property { get; }

    public A(object? property)
    {
        this.Property = property;
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            var obj = new object();
            dynamic instance = Activator.CreateInstance( type, obj )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );

            Assert.Null( deserialized.Property );

            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Same( obj, deserialized.Property );
        }

        [Fact]
        public void PrimitiveValueType()
        {
            // Verifies that a primitive value-type property is deserialized in constructor.
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
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type, 42 )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );

            Assert.Equal( 42, deserialized.Property );

            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 42, deserialized.Property );
        }

        [Fact]
        public void ValueTypeWithoutReference()
        {
            // Verifies that a value-type property is deserialized in constructor.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public S Property { get; }

    public A(int x, int y)
    {
        this.Property = new S(){X=x, Y=y};
    }
}

public struct S : ICompileTimeSerializable
{
    public int X;
    public int Y;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type, 27, 42 )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );

            Assert.Equal( 27, deserialized.Property.X );
            Assert.Equal( 42, deserialized.Property.Y );

            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 27, deserialized.Property.X );
            Assert.Equal( 42, deserialized.Property.Y );
        }

        [Fact]
        public void ValueTypeContainingReference()
        {
            // Verifies that a value-type property containing a reference is deserialized in DeserializeFields method even if it was readonly.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public S Property { get; }

    public A(int x, object y)
    {
        this.Property = new S(){X=x, Y=y};
    }
}

public struct S : ICompileTimeSerializable
{
    public int X;
    public object? Y;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            var obj = new object();
            dynamic instance = Activator.CreateInstance( type, 42, obj )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );

            Assert.Equal( 0, deserialized.Property.X );
            Assert.Null( deserialized.Property.Y );

            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 42, deserialized.Property.X );
            Assert.Same( obj, deserialized.Property.Y );
        }

        [Fact]
        public void ValueTypeIndirectlyContainingReference()
        {
            // Verifies that a value-type property indirectly containing a reference is deserialized in DeserializeFields method even if it was readonly.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public S Property { get; }

    public A(int x, object y)
    {
        this.Property = new S() { A = new T() { X=x, Y=y }, B = new U() { X=x, Y=y } };
    }
}

public struct S : ICompileTimeSerializable
{
    public T A;
    public U B {get;set;}
}

public struct T : ICompileTimeSerializable
{
    public int X;
    public object? Y;
}

public struct U : ICompileTimeSerializable
{
    public int X;
    public object? Y;
}

";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            var obj = new object();
            dynamic instance = Activator.CreateInstance( type, 42, obj )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );

            Assert.Equal( 0, deserialized.Property.A.X );
            Assert.Null( deserialized.Property.A.Y );
            Assert.Equal( 0, deserialized.Property.B.X );
            Assert.Null( deserialized.Property.B.Y );

            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 42, deserialized.Property.A.X );
            Assert.Same( obj, deserialized.Property.A.Y );
            Assert.Equal( 42, deserialized.Property.B.X );
            Assert.Same( obj, deserialized.Property.B.Y );
        }

        [Fact]
        public void ValueTypeContainingNonSerializedReference()
        {
            // Verifies that a value-type property containing a non-serialized reference is deserialized in constructor even if it was readonly.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public S Property { get; }

    public A(int x, object y)
    {
        this.Property = new S(){X=x, Y=y};
    }
}

public struct S : ICompileTimeSerializable
{
    public int X;

    [NonCompileTimeSerialized]
    public object? Y;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            var obj = new object();
            dynamic instance = Activator.CreateInstance( type, 42, obj )!;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );

            Assert.Equal( 42, deserialized.Property.X );

            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 42, deserialized.Property.X );
        }
    }
}