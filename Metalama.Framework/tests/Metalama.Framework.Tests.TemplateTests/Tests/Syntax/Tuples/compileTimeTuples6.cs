using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Tuples.CompileTimeTuples6
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            (int a, byte b) left = meta.CompileTime( ( 5, (byte)10 ) );
            Console.WriteLine( left.a );

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