using System;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Throw.ThrowExpressionBody
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template() => throw new Exception();
    }

    internal class TargetCode
    {
        private void Method( int a )
        {
            Console.WriteLine( "Hello, world." );
        }
    }
}