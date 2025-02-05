#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.ReturnStatements.ReturnVoidProceedAndDefault
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            try
            {
                return meta.Proceed();
            }
            catch
            {
                return default;
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