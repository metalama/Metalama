using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Deferred_;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var someType = new Promise<INamedType>();
        builder.IntroduceMethod( nameof(MethodTemplate), args: new { someType } );
        someType.Value = builder.Target;
    }

    [Template]
    private void MethodTemplate( INamedType someType )
    {
        Console.WriteLine( someType.ToString() );
    }
}

// <target>
[TheAspect]
internal class C { }