// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.NullConditionalAssignment_MixedScope;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var field = meta.Target.Type.Fields.FirstOrDefault();

        // This should fail with LAMA0259 - compile-time receiver (field), run-time access (.Value)
        var value = field?.Value;

        return value;
    }
}

internal class TargetCode
{
    public int _field;

    [Aspect]
    private int Method( int a )
    {
        return a;
    }
}
