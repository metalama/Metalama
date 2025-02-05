using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.UnsupportedSyntax.StatementSimpleLambdaNotSupported
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            Action<object?> action = p => { Console.WriteLine( p?.ToString() ); };

            var result = meta.Proceed();

            action( result );

            return result;
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}