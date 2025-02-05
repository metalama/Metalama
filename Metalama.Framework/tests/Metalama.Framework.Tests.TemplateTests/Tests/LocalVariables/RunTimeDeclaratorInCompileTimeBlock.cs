using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.RunTimeDeclaratorInCompileTimeBlock
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            if (meta.Target.Parameters.Count > 0)
            {
                var x = 0;
                Console.WriteLine( x );
            }

            foreach (var p in meta.Target.Parameters)
            {
                var y = 0;
                Console.WriteLine( y );
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