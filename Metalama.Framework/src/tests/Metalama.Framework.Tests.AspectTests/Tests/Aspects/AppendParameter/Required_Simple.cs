// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Required_Simple;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors )
        {
            // No defaultValue supplied => required parameter.
            builder.With( constructor ).IntroduceParameter( "p", typeof(int) );
        }
    }
}

// <target>
[MyAspect]
public class A
{
    public A( int x )
    {
        this.X = x;
    }

    public int X { get; }
}
