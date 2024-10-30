using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.IO;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MultipleConstraints;

internal class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(I) );
    }

    [Introduce]
    private void M<T1, T2, T3, T4>()
        where T1 : class, new()
        where T2 : Stream
        where T3 : unmanaged
        where T4 : struct { }

    [InterfaceMember]
    public void IM<T1, T2, T3, T4>() { }
}

internal interface I
{
    void IM<T1, T2, T3, T4>()
        where T1 : class, new()
        where T2 : Stream
        where T3 : unmanaged
        where T4 : struct;
}

// <target>
[MyAspect]
internal class C { }