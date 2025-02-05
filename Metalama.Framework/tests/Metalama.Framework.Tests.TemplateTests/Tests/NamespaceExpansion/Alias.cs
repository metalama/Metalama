using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using MyMath = System.Math;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Templating.Alias
{
    namespace UsingStatic
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                Console.Write( MyMath.PI );

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