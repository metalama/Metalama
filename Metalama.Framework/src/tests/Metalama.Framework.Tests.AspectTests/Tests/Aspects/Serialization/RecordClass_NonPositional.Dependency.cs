// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_NonPositional;

//<target>
[RunTimeOrCompileTime]
public record class SerializableClass : ICompileTimeSerializable
{
    public int Foo { get; set; }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableClass SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new SerializableClass() { Foo = 42 };
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