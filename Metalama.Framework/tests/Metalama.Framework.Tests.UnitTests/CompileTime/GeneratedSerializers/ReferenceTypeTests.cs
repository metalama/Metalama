// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers;

public sealed class ReferenceTypeTests : SerializerTestBase
{
    // Tests verify correctness of the generated serializer.

    [Fact]
    public void MutableMembers()
    {
        // Verifies that serializable type with mutable members can be serialized and deserialized.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Field;
    public int Property { get; set; }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var type = project.GetType( "A" );
        var lamaSerializer = GetSerializer( type );

        dynamic instance = Activator.CreateInstance( type )!;
        instance.Field = 13;
        instance.Property = 42;

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

    [Fact]
    public void ReadOnlyValueTypeMembers()
    {
        // Verifies that serializable type with read-only members can be serialized and deserialized.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
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

    [Fact]
    public void ReadOnlyReferenceTypeMembers()
    {
        // Verifies that serializable type with read-only members can be serialized and deserialized.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public readonly B Field;
    public B Property { get; }

    public A(B field, B property)
    {
        this.Field = field;
        this.Property = property;
    }
}

public class B
{
    public readonly int Field;

    public B(int field)
    {
        this.Field = field;
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var typeA = project.GetType( "A" );
        var typeB = project.GetType( "B" );
        var lamaSerializer = GetSerializer( typeA );

        dynamic instanceB1 = Activator.CreateInstance( typeB, 13 )!;
        dynamic instanceB2 = Activator.CreateInstance( typeB, 42 )!;
        var instanceA = Activator.CreateInstance( typeA, instanceB1, instanceB2 )!;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instanceA, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( typeA, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

        Assert.Equal( 13, deserialized.Field.Field );
        Assert.Equal( 42, deserialized.Property.Field );
    }

    [Fact]
    public void InitOnlyReferenceTypeMembers()
    {
        // Verifies that serializable type with init-only members can be serialized and deserialized.

        // ReSharper disable once ConvertToConstant.Local
        var code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]

public class A : ICompileTimeSerializable
{
    public B Property { get; init; }

    public A(B property)
    {
        this.Property = property;
    }
}

public class B
{
    public readonly int Field;

    public B(int field)
    {
        this.Field = field;
    }
}
";

#if NETFRAMEWORK
        code += "namespace System.Runtime.CompilerServices { internal static class IsExternalInit {}}";
#endif

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var typeA = project.GetType( "A" );
        var typeB = project.GetType( "B" );
        var lamaSerializer = GetSerializer( typeA );

        dynamic instanceB = Activator.CreateInstance( typeB, 42 )!;
        var instanceA = Activator.CreateInstance( typeA, instanceB )!;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instanceA, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( typeA, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

        Assert.Equal( 42, deserialized.Property.Field );
    }

    [Fact]
    public void ExplicitParameterlessConstructor()
    {
        // Verifies that serializable compile-time type with explicit parameterless constructor can be serialized and deserialized.
        // Generator should not inject parameterless constructor when it is already defined.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public A()
    {
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var type = project.GetType( "A" );
        var lamaSerializer = GetSerializer( type );

        dynamic instance = Activator.CreateInstance( type )!;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );
    }

    [Fact]
    public void GenericClass()
    {
        // Verifies that serializable generic type can be serialized and deserialized.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A<T> : ICompileTimeSerializable
{
    public T Property { get; }

    public A(T property)
    {
        this.Property = property;
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var type = project.GetType( "A`1" ).MakeGenericType( typeof(int) );
        var lamaSerializer = GetSerializer( type );

        dynamic instance = Activator.CreateInstance( type, 42 )!;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

        Assert.Equal( 42, deserialized.Property );
    }

    [Fact]
    public void ClosedGenericValue()
    {
        // Verifies that generated serializer correctly handles a field/property of closed generic type.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A<T>
{
    public T Property { get; set; }

    public A(T property)
    {
        this.Property = property;
    }
}

public class B : ICompileTimeSerializable
{
    public A<object> Field;
    public A<int> Property { get; set; }

    public B(A<object> field, A<int> property)
    {
        this.Field = field;
        this.Property = property;
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var typeAObject = project.GetType( "A`1" ).MakeGenericType( typeof(object) );
        var typeAInt = project.GetType( "A`1" ).MakeGenericType( typeof(int) );
        var typeB = project.GetType( "B" );
        var lamaSerializer = GetSerializer( typeB );

        var obj = new object();
        dynamic instanceAObj = Activator.CreateInstance( typeAObject, obj )!;
        dynamic instanceAInt = Activator.CreateInstance( typeAInt, 42 )!;
        var instanceB = Activator.CreateInstance( typeB, instanceAObj, instanceAInt )!;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instanceB, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( typeB, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

        Assert.Same( instanceAObj, deserialized.Field );
        Assert.Same( instanceAInt, deserialized.Property );
    }

    [Fact]
    public void OpenGenericValue()
    {
        // Verifies that generated serializer correctly handles a field/property of open generic type.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A<T>
{
    public T Property { get; set; }

    public A(T property)
    {
        this.Property = property;
    }
}

public class B<T> : ICompileTimeSerializable
{
    public A<T> Property { get; set; }

    public B(A<T> property)
    {
        this.Property = property;
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var typeA = project.GetType( "A`1" ).MakeGenericType( typeof(int) );
        var typeB = project.GetType( "B`1" ).MakeGenericType( typeof(int) );
        var lamaSerializer = GetSerializer( typeB );

        dynamic instanceA = Activator.CreateInstance( typeA, 42 )!;
        var instanceB = Activator.CreateInstance( typeB, instanceA )!;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instanceB, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( typeB, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

        Assert.Same( instanceA, deserialized.Property );
    }

    [Fact]
    public void Nested()
    {
        // Verifies that serializable nested type can be serialized and deserialized.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A
{
    public class B : ICompileTimeSerializable
    {
        public int Field;
        public int Property { get; set; }
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var type = project.GetType( "A+B" );
        var lamaSerializer = GetSerializer( type );

        dynamic instance = Activator.CreateInstance( type )!;
        instance.Field = 13;
        instance.Property = 42;

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

    [Fact]
    public void NestedGeneric()
    {
        // Verifies that serializable nested generic type can be serialized and deserialized.
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A<T>
{
    public class B<U> : ICompileTimeSerializable
    {
        public T Field;
        public U Property { get; set; }
    }
}
";

        using var testContext = this.CreateTestContext();
        var domain = testContext.Domain;

        var project = CreateCompileTimeProject( domain, testContext, code );

        var type = project.GetType( "A`1+B`1" ).MakeGenericType( typeof(int), typeof(double) );
        var lamaSerializer = GetSerializer( type );

        dynamic instance = Activator.CreateInstance( type )!;
        instance.Field = 13;
        instance.Property = 42.0;

        var constructorArgumentsWriter = new TestArgumentsWriter();
        var initializationArgumentsWriter = new TestArgumentsWriter();
        lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

        var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
        var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

        dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
        lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

        Assert.Equal( 13, deserialized.Field );
        Assert.Equal( 42.0, deserialized.Property );
    }
}