// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Simple;

/*
 * Tests single OverrideConstructor advice with trivial template on constructors.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).Override( nameof(Template) );
        }
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is the override." );

        foreach (var param in meta.Target.Parameters)
        {
            Console.WriteLine( $"Param {param.Name} = {param.Value}" );
        }

        meta.Proceed();
    }
}

public class BaseClass
{
    public BaseClass( int x ) { }
}

// <target>
[Override]
public class TargetClass : BaseClass
{
    public TargetClass( int x, string s ) : base( x )
    {
        Console.WriteLine( $"This is the original constructor." );
    }

    public TargetClass() : this( 42, "42" )
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}