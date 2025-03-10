// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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