using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.New.CompileTimeNewClass
{
    [CompileTime]
    internal class CompileTimeClass
    {
        public string String = "string";
    }

    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var c = meta.CompileTime( new CompileTimeClass() );
            Console.WriteLine( c.String );

            var c1 = new CompileTimeClass();
            Console.WriteLine( c1.String );

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