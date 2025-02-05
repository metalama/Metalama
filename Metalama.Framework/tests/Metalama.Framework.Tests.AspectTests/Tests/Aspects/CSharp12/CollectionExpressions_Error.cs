#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Generic;

#if TESTRUNNER
using System;
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.CollectionExpressions_Error;

// This is an error because of a bug in Roslyn, see https://github.com/dotnet/roslyn/issues/69704.

public class TheAspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );
    }

    public override dynamic? OverrideMethod()
    {
#if TESTRUNNER
        int[] collection = [1, 2, 3, ..meta.Target.Parameters[0].Value];
        Console.WriteLine(collection);
#endif

        return meta.Proceed();
    }
}

public class C
{
    // <target>
    [TheAspect]
    private static void M( List<int> numbers ) { }
}

#endif