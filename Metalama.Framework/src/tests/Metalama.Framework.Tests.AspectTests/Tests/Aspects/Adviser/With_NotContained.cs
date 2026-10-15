// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Adviser.With_NotContained;

// The adviser returned by WithDeclaringType targets C. D.N is not contained in C, so With must throw.
public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var otherMethod = builder.Target.Compilation.Types.OfName( "D" ).Single().Methods.OfName( "N" ).Single();

        builder.WithDeclaringType().With( otherMethod ).Override( nameof(Template) );
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
}

// <target>
internal class D
{
    public void N() { }
}
