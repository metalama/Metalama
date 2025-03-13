// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_ValueTypeReference;

[RunTimeOrCompileTime]
public class ReferenceType : ICompileTimeSerializable
{
    public ValueType Value;

    public ReferenceType( int value )
    {
        Value.Value = new InnerReferenceType( value );
    }
}

[RunTimeOrCompileTime]
public struct ValueType : ICompileTimeSerializable
{
    public InnerReferenceType Value { get; set; }
}

[RunTimeOrCompileTime]
public class InnerReferenceType : ICompileTimeSerializable
{
    public int Value { get; set; }

    public InnerReferenceType( int value )
    {
        Value = value;
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public ReferenceType SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new ReferenceType( x );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.Value.Value.Value ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}