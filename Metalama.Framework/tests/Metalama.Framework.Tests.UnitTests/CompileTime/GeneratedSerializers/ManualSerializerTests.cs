// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public sealed class ManualSerializerTests : SerializerTestBase
    {
        [Fact]
        public void BaseParameterlessConstructor()
        {
            // Verifies that custom serializer (with constructor phase) defined in the base type is correctly consumed by the generated serializer in the derived type.
            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int BaseFieldConstructor;
    public int BaseFieldInitialization;

    public A()
    {
    }

    public A(IArgumentsReader constructorArguments)
    {
        this.BaseFieldConstructor = constructorArguments.GetValue<int>(""BaseFieldConstructor"");
    }

    public class CustomSerializer : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
        {
            return new A();
        }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var a = (A)obj;
            constructorArguments.SetValue(""BaseFieldConstructor"", a.BaseFieldConstructor + 1);
            initializationArguments.SetValue(""BaseFieldInitialization"",a.BaseFieldInitialization + 1);
        }

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
        {
            var a = (A)obj;
            a.BaseFieldInitialization = initializationArguments.GetValue<int>(""BaseFieldInitialization"");
        }
    }
}

public class B : A
{
    public int DerivedField;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "B" );
            var serializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            instance.BaseFieldConstructor = 12;
            instance.BaseFieldInitialization = 26;
            instance.DerivedField = 42;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            serializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = serializer.CreateInstance( type, constructorArgumentsReader );
            serializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.BaseFieldConstructor );
            Assert.Equal( 27, deserialized.BaseFieldInitialization );
            Assert.Equal( 42, deserialized.DerivedField );
        }

        [Fact]
        public void BaseDeserializingConstructor()
        {
            // Verifies that custom serializer (without constructor phase) defined in the base type is correctly consumed by the generated serializer in the derived type.
            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int BaseField;

    public A()
    {
    }

    public class CustomSerializer : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
        {
            return new A();
        }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var a = (A)obj;
            initializationArguments.SetValue(""BaseField"",a.BaseField + 1);
        }

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
        {
            var a = (A)obj;
            a.BaseField = initializationArguments.GetValue<int>(""BaseField"");
        }
    }
}

public class B : A
{
    public int DerivedField;
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "B" );
            var serializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            instance.BaseField = 12;
            instance.DerivedField = 42;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            serializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = serializer.CreateInstance( type, constructorArgumentsReader );
            serializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.BaseField );
            Assert.Equal( 42, deserialized.DerivedField );
        }

        [Fact]
        public void ExistingSerializer()
        {
            // Verifies that custom serializer defined in the base type is correctly consumed by the generated serializer in the derived type.
            const string code = @"
using System;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Serialization;
[assembly: CompileTime]
public class A : ICompileTimeSerializable
{
    public int Field;

    public class CustomSerializer : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
        {
            var o = new A();
            o.Field = constructorArguments.GetValue<int>(""Field"");
            return o;
        }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var a = (A)obj;
            constructorArguments.SetValue(""Field"",a.Field + 1);
        }

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
        {
        }
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A" );
            var serializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            instance.Field = 12;

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            serializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = serializer.CreateInstance( type, constructorArgumentsReader );
            serializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, deserialized.Field );
        }
    }
}