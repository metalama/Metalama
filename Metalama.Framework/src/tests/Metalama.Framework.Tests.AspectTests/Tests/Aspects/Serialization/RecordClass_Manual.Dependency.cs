// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_Manual;

//<target>
[RunTimeOrCompileTime]
public record class SerializableClass( int Foo ) : ICompileTimeSerializable
{
    public class Serializer_Custom : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
            => new SerializableClass( constructorArguments.GetValue<int>( "Foo" ) );

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            => constructorArguments.SetValue( "Foo", ( (SerializableClass)obj ).Foo );
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableClass SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new SerializableClass( 42 );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.Foo ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}