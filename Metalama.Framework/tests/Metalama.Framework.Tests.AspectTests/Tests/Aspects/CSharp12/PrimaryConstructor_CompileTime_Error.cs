#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.PrimaryConstructor_CompileTime;

public class TheAspect( int x ) : MethodAspect
{
    private int _x = x;

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(M) );
    }

    [Template]
    public void M()
    {
        Console.WriteLine( _x );
        meta.Proceed();
    }
}

// <target>
public class C
{
    [TheAspect( 42 )]
    public void M() { }
}

#endif