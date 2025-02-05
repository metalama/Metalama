using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.TryCatchFinally.ExceptionFilterRunTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            try
            {
                Console.WriteLine( meta.Target.Parameters.Count );

                return meta.Proceed();
            }
            catch (Exception e) when (e.GetType().Name.Contains( "DivideByZero" ))
            {
                return -1;
            }
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return 42 / a;
        }
    }
}