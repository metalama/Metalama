using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.RunTimeToCompileTimeAssignmentExpression
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = 0;
            var y = meta.CompileTime( 0 );

            x = y = 1;

            meta.InsertComment( "y = " + y.ToString() );

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