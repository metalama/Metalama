using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Pragma.Comments
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            meta.InsertComment( "Oops 1", null, "Oops 2", "Multi\nline" );

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