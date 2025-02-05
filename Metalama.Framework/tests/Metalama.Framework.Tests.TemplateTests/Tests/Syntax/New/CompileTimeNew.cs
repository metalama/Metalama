using System;
using System.Text;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.New.CompileTimeNew
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var o = meta.CompileTime( new StringBuilder() );
            o.Append( "x" );
            Console.WriteLine( o.ToString() );

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