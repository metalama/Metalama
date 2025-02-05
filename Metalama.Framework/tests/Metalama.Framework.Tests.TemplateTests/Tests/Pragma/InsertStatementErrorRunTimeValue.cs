using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Pragma.InsertStatementErrorRunTimeValue
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            meta.InsertStatement( meta.This );

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