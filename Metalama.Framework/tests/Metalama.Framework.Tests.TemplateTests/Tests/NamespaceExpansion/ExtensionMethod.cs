using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion
{
    namespace ExtensionMethod
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                var list = new List<int>();

                // No argument.
                var max = list.Max();

                // Constant argument.
                var take = list.Take( 1 );

                // Dynamic argument.
                var take2 = list.Take( (int)meta.Target.Parameters[0].Value );

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
}