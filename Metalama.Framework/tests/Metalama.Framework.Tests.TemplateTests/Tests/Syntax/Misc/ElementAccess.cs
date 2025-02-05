using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Misc.ElementAccess
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            Console.WriteLine( meta.This[0] );
            Console.WriteLine( meta.Target.Parameters[0].Value );

            return 0;
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