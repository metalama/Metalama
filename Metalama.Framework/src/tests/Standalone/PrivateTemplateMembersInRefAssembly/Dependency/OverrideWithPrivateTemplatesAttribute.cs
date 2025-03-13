// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Dependency;

public class OverrideWithPrivateTemplatesAttribute : TypeAspect
{
    
    [Template]
    private dynamic? MethodTemplate() 
    {
        Console.WriteLine( "Overridden." );
        return meta.Proceed();
    }

    [Template]
    private dynamic? PropertyTemplate 
    {
        get
        {
            Console.WriteLine( "Overridden." );
            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( "Overridden." );
            meta.Proceed();
        }
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var method in builder.Target.Methods )
        {
            builder.Advice.Override( method, nameof( MethodTemplate ) );
        }
        foreach ( var property in builder.Target.Properties )
        {
            builder.Advice.Override( property, nameof( PropertyTemplate ) );
        }
    }



}
