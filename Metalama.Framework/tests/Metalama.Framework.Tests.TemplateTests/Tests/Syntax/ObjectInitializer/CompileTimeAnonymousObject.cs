using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.Misc.CompileTimeAnonymousObject
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = meta.CompileTime( new { Arg0 = meta.Target.Parameters[0].Name, Count = meta.Target.Parameters.Count } );

            Console.WriteLine( x.Arg0 );

            var result = meta.Proceed();

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