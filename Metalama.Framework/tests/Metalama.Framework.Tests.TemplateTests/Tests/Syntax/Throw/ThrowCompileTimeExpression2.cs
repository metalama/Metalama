using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Throw.ThrowCompileTimeExpression2
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time
            var r = meta.CompileTime<object>( null );

            var t = r ?? throw new Exception();

            return null;
        }
    }

    internal class TargetCode
    {
        private void Method( int a )
        {
            Console.WriteLine( "Hello, world." );
        }
    }
}