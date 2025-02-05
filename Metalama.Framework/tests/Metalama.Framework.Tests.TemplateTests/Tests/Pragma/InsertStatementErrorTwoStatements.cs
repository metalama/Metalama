using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Pragma.InsertStatementErrorTwoStatements
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            meta.InsertStatement( "for (;;) {} return 1;" );

            return default;
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