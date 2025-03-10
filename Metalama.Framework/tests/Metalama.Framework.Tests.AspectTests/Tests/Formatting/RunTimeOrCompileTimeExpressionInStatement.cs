// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.RunTimeOrCompileTimeExpressionInStatement;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        if (true)
        {
        }

        if ((true))
        {
        }

#if TESTRUNNER
        if (()meta.Proceed())
        {
        }
#endif

        foreach (var x in new[] { 42 })
        {
        }

        do
        {

        } while (false);

        while (true)
        {
        }
    }
}