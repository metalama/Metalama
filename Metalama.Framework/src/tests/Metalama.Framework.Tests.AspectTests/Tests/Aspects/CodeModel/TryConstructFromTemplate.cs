// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.TryConstructFromTemplate;

[AttributeUsage( AttributeTargets.Method )]
[RunTimeOrCompileTime]
public class MyCustomAttribute : Attribute
{
    public string Name { get; }

    public MyCustomAttribute( string name )
    {
        Name = name;
    }
}

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var attribute = meta.Target.Method.Attributes.OfAttributeType( typeof(MyCustomAttribute) ).FirstOrDefault();

        // Test non-generic TryConstruct from a template.
        if ( attribute != null && attribute.TryConstruct( out var constructedNonGeneric ) && constructedNonGeneric is MyCustomAttribute myAttr )
        {
            Console.WriteLine( $"TryConstruct: {myAttr.Name}" );
        }

        // Test non-generic Construct from a template.
        if ( attribute != null )
        {
            var constructed = (MyCustomAttribute) attribute.Construct();
            Console.WriteLine( $"Construct: {constructed.Name}" );
        }

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [TestAspect]
    [MyCustomAttribute( "Hello" )]
    private void M() { }
}
