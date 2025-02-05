using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.NameClashWithMemberAccess
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var n = meta.Target.Parameters.Count; // build-time

            if (n == 1)
            {
                var WriteLine = 0;
                Console.WriteLine( WriteLine );
            }

            if (n == 1)
            {
                var WriteLine = 1;
                Console.WriteLine( WriteLine );
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