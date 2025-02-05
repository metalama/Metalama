using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.Template_Method;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void Method()
    {
        Console.WriteLine( """method""" );
    }
}

// <target>
[TheAspect]
internal class Target { }