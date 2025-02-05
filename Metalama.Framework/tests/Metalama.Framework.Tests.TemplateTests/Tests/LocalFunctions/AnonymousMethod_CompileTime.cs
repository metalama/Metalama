using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.AnonymousMethod_CompileTime;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var func = meta.CompileTime( delegate() { return "Hello, world."; } );

        Console.WriteLine( func() );

        return null;
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}