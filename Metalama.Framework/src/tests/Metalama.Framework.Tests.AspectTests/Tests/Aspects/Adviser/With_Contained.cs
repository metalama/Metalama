// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Adviser.With_Contained;

// The adviser returned by WithDeclaringType targets C. C.N is contained in C, so With must succeed.
public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var typeAdviser = builder.WithDeclaringType();
        var otherMethod = typeAdviser.Target.Methods.OfName( "N" ).Single();

        typeAdviser.With( otherMethod ).Override( nameof(Template) );
        typeAdviser.With( typeAdviser.Target ).IntroduceField( "_introducedField", typeof(int) );
    }

    [Template]
    public dynamic? Template()
    {
        Console.WriteLine( "Overridden." );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [MyAspect]
    public void M() { }

    public void N() { }
}
