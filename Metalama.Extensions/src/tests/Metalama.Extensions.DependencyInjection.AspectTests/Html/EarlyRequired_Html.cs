#if TEST_OPTIONS
// @ClearIgnoredDiagnostics
#endif

#if !TESTRUNNER
#pragma warning disable CS0649
#pragma warning disable CS8618
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Html.EarlyRequired_Html;

// This test verifies that CS0649/CS8618 suppressions from [IntroduceDependency] are correctly applied to input HTML.
// The [IntroduceDependency] attribute should suppress CS0649 (field never assigned) and CS8618 on the aspect field.
// Expected: CS0649 and CS8618 should NOT appear in the input HTML because the aspect suppresses them.

// <target>
public class TargetClass
{
    [LogAspect]
    public void Method() { }
}

public class LogAspect : OverrideMethodAspect
{
    [IntroduceDependency]
    private readonly IFormatProvider _formatProvider;

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( this._formatProvider );

        return meta.Proceed();
    }
}
