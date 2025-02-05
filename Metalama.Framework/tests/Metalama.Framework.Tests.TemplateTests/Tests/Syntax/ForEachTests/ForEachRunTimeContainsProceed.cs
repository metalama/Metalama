#pragma warning disable CS8600, CS8603
using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachRunTimeContainsProceed
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var array = Enumerable.Range( 1, 2 );

            foreach (var i in array)
            {
                return meta.Proceed();
            }

            return null;
        }
    }

    internal class TargetCode
    {
        private void Method( int a, int bb )
        {
            Console.WriteLine( a + bb );
        }
    }
}