// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Adviser.With_Contained;

// WithTemplateProvider returns an adviser for C. With accepts C itself and C.N, which is contained in C.
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var typeAdviser = builder.WithTemplateProvider( new Templates() );

        typeAdviser.With( typeAdviser.Target.Methods.OfName( "N" ).Single() ).Override( nameof(Templates.OverrideTemplate) );
        typeAdviser.With( typeAdviser.Target ).IntroduceField( "_introducedField", typeof(int) );
    }
}

public class Templates : ITemplateProvider
{
    [Template]
    public dynamic? OverrideTemplate()
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

    public void N() { }
}
