using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedAsyncAsyncDefaultTemplate;

internal class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Template) );
    }

    [Template]
    private async Task Template()
    {
        await meta.ProceedAsync();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task AsyncTaskMethod()
    {
        await Task.Yield();
    }
}