using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Throw.ThrowCompileTimeExpression
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time
            var r = meta.CompileTime<object>( null );

            // The next condition should not be reduced at build time because this would result into invalid syntax.
            var s = r != null ? 1 : throw new Exception();

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