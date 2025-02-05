using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.CompileTimeSimpleLambda
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var action = meta.CompileTime( new Func<int, int>( x => x + 1 ) );

            var result = meta.CompileTime( 1 );

            result = action( result );

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