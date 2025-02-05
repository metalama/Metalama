#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.CompileTimeValue;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var nn = TypedConstant.Create( "foo" );

        Console.WriteLine( nn.Value?.ToString() );
        Console.WriteLine( nn.Value!.ToString() );

        return null!;
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void M() { }
}