using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32900;

public sealed class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var result = meta.Default( meta.Target.Method.GetAsyncInfo().ResultType );

        try
        {
            result = meta.Proceed();
        }
        catch { }

        return result;
    }
}

// <target>
public partial class TargetClass
{
    [TestAspect]
    public async Task AsyncTaskMethod()
    {
        var result = 42;
        await Task.Yield();
        _ = result;
    }

    [TestAspect]
    public async Task<int> AsyncTaskIntMethod()
    {
        var result = 42;
        await Task.Yield();

        return result;
    }
}