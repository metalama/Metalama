#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicRunTimeValueExclamationMark;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var nn = meta.CompileTime( "foo" );

        Console.WriteLine( meta.RunTime( nn )!.ToString() );

        var n = meta.CompileTime( (string?)"bar" );

        Console.WriteLine( meta.RunTime( n )!.ToString() );

        return null!;
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void M() { }
}