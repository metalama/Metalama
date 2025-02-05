using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters;

internal class Aspect : TypeAspect
{
    [Introduce]
    private int Add( int a )
    {
        AddImpl( a, 1, meta.CompileTime( 1 ), 1, meta.CompileTime( 1 ) );

        throw new Exception();
    }

    [Template]
    private void AddImpl( int a, int b, int c, [CompileTime] int d, [CompileTime] int e )
    {
        meta.Return( a + b + c + d + e );
    }
}

// <target>
[Aspect]
internal class TargetCode { }