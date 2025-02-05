using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Tuples.CompileTimeTuples4
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var a = meta.CompileTime( 1 );
            var b = meta.CompileTime( 2 );

            var namedItems = meta.CompileTime( ( a, b ) );
            Console.WriteLine( namedItems.a );

            return meta.Proceed();
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