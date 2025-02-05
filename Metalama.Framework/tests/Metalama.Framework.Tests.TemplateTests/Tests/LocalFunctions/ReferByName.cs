using System.Threading;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.ReferByName;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private void Template()
    {
        void TheLocalFunction( object? state )
        {
            meta.Proceed();
        }

        ThreadPool.QueueUserWorkItem( TheLocalFunction );
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}