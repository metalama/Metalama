#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.ReturnStatements.ReturnVoidResultAndNull
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            try
            {
                var result = meta.Proceed();

                return result;
            }
            catch
            {
                return null;
            }
        }
    }

    internal class TargetCode
    {
        // <target>
        private void Method( int a, int b )
        {
            Console.WriteLine( a / b );
        }
    }
}