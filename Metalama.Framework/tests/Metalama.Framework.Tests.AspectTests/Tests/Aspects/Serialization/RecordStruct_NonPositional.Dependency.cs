// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordStruct_NonPositional;

//<target>
[RunTimeOrCompileTime]
public record struct SerializableStruct : ICompileTimeSerializable
{
    public int Foo { get; set; }

    public class Serializer_Custom : ValueTypeSerializer<SerializableStruct>
    {
        public override SerializableStruct DeserializeObject( IArgumentsReader initializationArguments )
        {
            SerializableStruct s = default;
            s.Foo = initializationArguments.GetValue<int>( "Foo" );

            return s;
        }

        public override void SerializeObject( SerializableStruct obj, IArgumentsWriter arguments )
            => arguments.SetValue( "Foo", ( (SerializableStruct)obj ).Foo );
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableStruct SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new SerializableStruct() { Foo = 42 };
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