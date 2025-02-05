// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public sealed class NonSerializedTests : SerializerTestBase
    {
        [Fact]
        public void Field()
        {
            // Verifies that serializable type with a non-serialized field can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    [NonCompileTimeSerialized]
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

            Assert.Equal( 0, deserialized.Field );
            Assert.Equal( 42, deserialized.Property );
        }

        [Fact]
        public void Property()
        {
            // Verifies that serializable type with a non-serialized property can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Field;
    [NonCompileTimeSerialized]
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
            Assert.Equal( 0, deserialized.Property );
        }

        [Fact]
        public void PropertyBackingField()
        {
            // Verifies that serializable type with a non-serialized auto-property backing field can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Field;
    [field:NonCompileTimeSerialized]
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
            Assert.Equal( 0, deserialized.Property );
        }

        [Fact]
        public void StaticProperty()
        {
            // Verifies that serializable type with a static property can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Field;
    
    public static int StaticProperty { get; set; }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            instance.Field = 13;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.Field );
        }

        [Fact]
        public void StaticField()
        {
            // Verifies that serializable type with a static field can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Field;
    
    public static int StaticField;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            instance.Field = 13;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.Field );
        }

        [Fact]
        public void NonAutoProperty()
        {
            // Verifies that serializable type with a non-auto property can be serialized and deserialized and the property is not used.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public B Field;
    public int ManualProperty { get => this.Field.Field; set => this.Field.Field = value; }
}

public class B : ICompileTimeSerializable
{
    public int Field;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var typeA = project.GetType( "A" );
            var typeB = project.GetType( "B" );
            var lamaSerializer = GetSerializer( typeA );

            dynamic instanceA = Activator.CreateInstance( typeA )!;
            dynamic instanceB = Activator.CreateInstance( typeB )!;
            instanceA.Field = instanceB;
            instanceB.Field = 42;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instanceA, constructorArgumentsWriter, initializationArgumentsWriter );

            instanceB.Field = -42;

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( typeA, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( instanceB, instanceA.Field );
            Assert.Equal( -42, instanceA.ManualProperty );
        }
    }
}