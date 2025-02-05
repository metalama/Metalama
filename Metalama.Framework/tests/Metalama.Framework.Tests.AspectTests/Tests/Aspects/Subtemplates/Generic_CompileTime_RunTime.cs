using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime_RunTime;

internal class Aspect : TypeAspect
{
    [Introduce]
    private void Introduced<T>()
    {
        Console.WriteLine( $"introduced T={typeof(T)}" );

        CalledTemplate2<T>();
        CalledTemplate2<T[]>();
        CalledTemplate2<List<T>>();
    }

    [Template]
    private void CalledTemplate2<[CompileTime] T>()
    {
        Console.WriteLine( $"called template T={typeof(T)}" );
    }
}

// <target>
[Aspect]
internal class TargetCode { }