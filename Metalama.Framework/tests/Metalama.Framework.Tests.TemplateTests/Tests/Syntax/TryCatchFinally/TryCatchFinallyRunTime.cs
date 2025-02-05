#pragma warning disable CS0162

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.TryCatchFinally.TryCatchFinallyRunTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = meta.CompileTime( 0 );

            try
            {
                Console.WriteLine( "try" );
                var result = meta.Proceed();
                Console.WriteLine( "success" );

                return result;
            }
            catch
            {
                Console.WriteLine( "exception " + x );

                throw;
            }
            finally
            {
                Console.WriteLine( "finally" );
            }

            Console.WriteLine( x );
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