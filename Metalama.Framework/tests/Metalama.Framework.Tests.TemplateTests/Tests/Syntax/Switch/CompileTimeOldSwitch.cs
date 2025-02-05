using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.OldSwitchCompileTime
{
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
}