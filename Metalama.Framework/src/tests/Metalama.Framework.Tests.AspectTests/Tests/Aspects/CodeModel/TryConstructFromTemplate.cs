// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.TryConstructFromTemplate;

[AttributeUsage( AttributeTargets.Method )]
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
        // Test TryConstruct<T> from a template.
        var attribute = meta.Target.Method.Attributes.OfAttributeType( typeof(MyCustomAttribute) ).FirstOrDefault();

        if ( attribute != null && attribute.TryConstruct<MyCustomAttribute>( out var constructed ) )
        {
            Console.WriteLine( $"Attribute Name: {constructed.Name}" );
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
