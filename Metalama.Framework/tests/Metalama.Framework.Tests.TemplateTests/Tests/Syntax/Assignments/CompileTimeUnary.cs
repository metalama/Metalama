using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.CompileTimeUnary
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = meta.CompileTime( 0 );
            x++;
            x--;
            ++x;
            --x;

            meta.InsertComment( "x = " + x.ToString() );

            return null;
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