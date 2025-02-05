using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfConstant;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private void Template()
    {
        if (true)
        {
            Console.WriteLine( "true" );
        }

        const bool c = true;

        if (c)
        {
            Console.WriteLine( "c" );
        }

        var b = true;

        if (b)
        {
            Console.WriteLine( "b" );
        }
    }
}

internal class TargetCode
{
    private void Method() { }
}