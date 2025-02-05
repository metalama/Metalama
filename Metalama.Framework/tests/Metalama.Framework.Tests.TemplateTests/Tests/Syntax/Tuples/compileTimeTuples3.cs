using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Tuples.CompileTimeTuples3
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            (int, string) anonymT = meta.CompileTime( ( 4, "" ) );
            Console.WriteLine( anonymT.Item1 );

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