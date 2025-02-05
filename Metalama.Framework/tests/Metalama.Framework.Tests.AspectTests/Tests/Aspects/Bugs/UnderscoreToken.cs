using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.UnderscoreToken;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void TheMethod()
    {
        // This uses to fail because of two parameters named _. 
        var x = meta.RunTime( new Action<string, string>( ( _, _ ) => { } ) );
    }
}

// <target>
[TheAspect]
internal class C;