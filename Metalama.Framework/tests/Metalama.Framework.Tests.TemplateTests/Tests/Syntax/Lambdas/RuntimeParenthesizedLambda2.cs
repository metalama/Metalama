using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.RuntimeParenthesizedLambda2
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var action = ( int a, int b ) => a + b;

            var result = meta.Proceed();

            result = action( result, 2 );

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