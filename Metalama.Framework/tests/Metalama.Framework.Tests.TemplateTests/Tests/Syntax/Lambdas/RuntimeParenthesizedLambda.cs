using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.RuntimeParenthesizedLambda
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var action = ( int x ) => x + 1;

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