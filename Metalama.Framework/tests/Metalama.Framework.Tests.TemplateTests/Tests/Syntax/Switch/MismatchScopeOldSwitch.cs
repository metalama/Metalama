using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.OldSwitchMismatchScope
{
    internal enum RunTimeEnum
    {
        one = 1,
        two = 2
    }

    [CompileTime]
    internal enum CompileTimeEnum
    {
        one = 1,
        two = 2
    }

    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            switch ((int)CompileTimeEnum.one)
            {
                case (int)RunTimeEnum.one:
                    Console.WriteLine( "1" );

                    break;

                case (int)RunTimeEnum.two:
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