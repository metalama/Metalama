using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters_SideEffects;

internal class Aspect : TypeAspect
{
    [Introduce]
    private int Add( int a )
    {
        AddImpl( 1, Add( 1 ), 1 );

        throw new Exception();
    }

    [Template]
    private void AddImpl( int a, int b, [CompileTime] int c )
    {
        meta.Return( a + b + c );
    }
}

// <target>
[Aspect]
internal class TargetCode { }