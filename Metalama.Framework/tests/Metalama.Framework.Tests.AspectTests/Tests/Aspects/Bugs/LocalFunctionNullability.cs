#pragma warning disable CS8321

using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.LocalFunctionNullability;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Task<object?> InvokeAsync()
        {
            return Task.FromResult<object?>( null );
        }

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    public void M() { }
}