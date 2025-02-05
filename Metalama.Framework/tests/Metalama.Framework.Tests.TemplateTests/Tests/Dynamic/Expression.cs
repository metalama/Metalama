using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0618

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.Expression
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = meta.RunTime( meta.Target.Method.Name );
            var exp2 = (IExpression)meta.Target.Parameters[0].Value!;
            exp2.Value = 5;

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}