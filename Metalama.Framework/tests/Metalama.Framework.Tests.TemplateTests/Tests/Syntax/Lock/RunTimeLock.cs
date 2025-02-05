using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lock.RunTimeLock
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            lock (meta.This)
            {
                var x = meta.Target.Parameters.Count;
                Console.WriteLine( x );

                return meta.Proceed();
            }
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