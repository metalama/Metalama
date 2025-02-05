using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.TryCatchFinally.TryCatchFinallyCompileTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var n = meta.CompileTime( 1 );

            try
            {
                n = 2;
            }
            catch
            {
                Console.WriteLine( meta.Target.Parameters.Count );
            }
            finally
            {
                Console.WriteLine( meta.Target.Parameters.Count );
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