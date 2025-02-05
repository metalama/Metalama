using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.RunTimeOrCompileTimeOldSwitch;

[RunTimeOrCompileTime]
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
        switch (SwitchEnum.one)
        {
            case SwitchEnum.one:
                Console.WriteLine( "1" );

                break;

            case SwitchEnum.two:
                Console.WriteLine( "2" );

                break;

            default:
                break;
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