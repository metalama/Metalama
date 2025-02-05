#if TEST_OPTIONS
// @FormatCompileTimeCode(false)
#endif

#pragma warning disable CS1998

using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Async2;

public class RetryAttribute : OverrideMethodAspect
{
    // Template for non-async methods.
    public override dynamic? OverrideMethod()
    {
        var result = meta.Proceed();

        return result;
    }

    // Template for async methods.
    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        async Task<object?> ExecuteCoreAsync()
        {
            var result = await meta.ProceedAsync();

            return result;
        }

        return await Task.Run( ExecuteCoreAsync );
    }
}

// <target>
internal class C
{
    [Retry]
    private void Foo() { }

    [Retry]
    private async Task FooAsync() { }
}