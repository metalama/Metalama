using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion
{
    namespace FullyQualified
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                Console.WriteLine( "Oops" );

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