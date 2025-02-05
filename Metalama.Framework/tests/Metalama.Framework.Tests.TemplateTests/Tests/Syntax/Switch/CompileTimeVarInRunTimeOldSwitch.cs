using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.OldSwitchChangeCompileTimeVarInRunTime
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var i = 0;
            var compileTimeVar = meta.CompileTime( 1 );

            switch (i)
            {
                case 0:
                    compileTimeVar += 1;
                    Console.WriteLine( compileTimeVar );

                    break;

                case 1:
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