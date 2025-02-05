using System.Text;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.SimpleCompileTimeAssignment
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = meta.CompileTime( 0 );
            x = 0;
            x += 4;
            x *= 2;
            x /= 2;
            x -= 2;
            x |= 128;
            x &= 127;

            var y = meta.CompileTime<StringBuilder>( null );
            y ??= new StringBuilder();
            y.Append( "yy" );

            meta.InsertComment( "x = " + x.ToString(), "y = " + y.ToString() );

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