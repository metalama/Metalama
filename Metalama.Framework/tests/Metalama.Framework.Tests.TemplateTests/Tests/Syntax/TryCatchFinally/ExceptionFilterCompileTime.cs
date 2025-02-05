using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.TryCatchFinally.ExceptionFilterCompileTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var a = meta.Target.Parameters.Count;
            var b = 0;

            try
            {
                meta.InsertComment( "comment" );
                Console.WriteLine( a );

                var x = 100 / 1;
                var y = x / a;
            }
            catch (Exception e) when (e.GetType().Name.Contains( "DivideByZero" ))
            {
                meta.InsertComment( "comment" );
                b = 1;
            }

            Console.WriteLine( b );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method()
        {
            return 42;
        }
    }
}