#if TEST_OPTIONS
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.StaticInterfaceMember;

internal class MyAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceMethod( nameof(Method), args: new { T = typeof(int) });
    }

    [Template]
    public void Method<[CompileTime] T>()
        where T : INumber<T>
    {
        Console.WriteLine(T.One);
    }
}

// <target>
[MyAspect]
internal class Target { }

#endif