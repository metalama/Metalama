// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Adviser.With_NotContained_IntroducedType;

// The adviser returned by IntroduceClass targets the introduced type. C.M is not contained in it, so With must throw.
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var nestedTypeAdviser = builder.IntroduceClass( "Nested" );

        nestedTypeAdviser.With( builder.Target.Methods.OfName( "M" ).Single() ).Override( nameof(Template) );
    }

    [Template]
    public dynamic? Template()
    {
        Console.WriteLine( "Overridden." );

        return meta.Proceed();
    }
}

// <target>
[MyAspect]
internal class C
{
    public void M() { }
}
