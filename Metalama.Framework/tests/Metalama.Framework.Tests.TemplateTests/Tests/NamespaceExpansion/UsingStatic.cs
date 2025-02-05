using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using static System.Math;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion
{
    namespace UsingStatic
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                Console.Write( PI );
                Console.Write( Max( 0, 1 ) );

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