using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Misc.ThisError
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            // Must be an error.
            meta.Cast( meta.Target.Type, this );

            return 0;
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