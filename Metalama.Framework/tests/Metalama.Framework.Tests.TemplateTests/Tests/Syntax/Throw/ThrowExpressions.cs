using System;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Throw.ThrowExpressions
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Run-time
            object? a = null;
            var b = a == null ? 1 : throw new Exception();
            var c = a ?? throw new Exception();

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