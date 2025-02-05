using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.CompileTimeOldSwitchWithThrow;

[CompileTime]
internal enum SwitchEnum
{
    one = 1,
    two = 2
}

internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var i = SwitchEnum.one;

        switch (i)
        {
            case SwitchEnum.one:
            case SwitchEnum.two:
                Console.WriteLine( "1 or 2" );

                break;

            default:
                throw new Exception();
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}