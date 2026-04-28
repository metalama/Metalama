// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.PrimaryConstructor_Runtime;

public class TheAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(this.M) );
    }

    [Template]
    public void M()
    {
        Console.WriteLine( "Aspect" );
        meta.Proceed();
    }
}

public class B( int x )
{
    public int X { get; set; } = x;
}

// <target>
public class C( int x ) : B( x )
{
    [TheAspect]
    public void M()
    {
        _ = new C( 42 );
    }
}