using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfParametersCount
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var b = meta.CompileTime( false );

            if (meta.Target.Parameters.Count > 0)
            {
                b = true;
            }
            else
            {
                b = false;
            }

            Console.WriteLine( b );

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