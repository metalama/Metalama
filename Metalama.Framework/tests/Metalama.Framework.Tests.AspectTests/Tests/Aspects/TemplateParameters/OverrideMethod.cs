// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.OverrideMethod;

public class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Method), args: new { a = 5, t = builder.Target.ReturnType } );
    }

    [Template]
    private void Method( [CompileTime] int a, IType t )
    {
        Console.WriteLine( a );
        Console.WriteLine( t.ToDisplayString() );
    }
}

// <target>
public class Target
{
    [Aspect]
    public void M() { }
}