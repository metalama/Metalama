// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ReturnVoidForNonVoid;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate();

        return default;
    }

    [Template]
    private void CalledTemplate()
    {
        meta.Return();
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private int Method()
    {
        return 42;
    }
}