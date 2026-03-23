// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeInterfaceInitOnlyProperty;

[CompileTime]
public interface IFace
{
    // Property with only an init setter and no getter.
    // Using string (reference type) to trigger Deserialize_MakeMutable path.
    string? InitOnlyValue { init; }
}

[RunTimeOrCompileTime]
public class ReferenceType : IFace, ICompileTimeSerializable
{
    // Implicitly implements IFace.InitOnlyValue (interface has no getter, class has both).
    public string? InitOnlyValue { get; init; }

    public ReferenceType( string? value )
    {
        InitOnlyValue = value;
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public ReferenceType SerializedValue;

    public TestAspect( string? x )
    {
        SerializedValue = new ReferenceType( x );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.InitOnlyValue ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( "hello" )]
    public virtual void Foo() { }
}
