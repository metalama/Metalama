// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Adviser.With_NotContained;

// WithTemplateProvider returns an adviser for C.M. C.N is not contained in C.M, so With must throw,
// even though C.N is contained in the target of the aspect.
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var methodAdviser = builder.With( builder.Target.Methods.OfName( "M" ).Single() ).WithTemplateProvider( new Templates() );

        methodAdviser.With( builder.Target.Methods.OfName( "N" ).Single() ).Override( nameof(Templates.OverrideTemplate) );
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
