// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using System;
using Metalama.Framework.Aspects;

#pragma warning disable IDE0062

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug580_DynamicInInterpolatedStringLocalFunction;

internal class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Dynamic local variable declared in the outer scope.
        var result = meta.Proceed();

        // Local function that captures the dynamic variable and uses it in an interpolated string.
        // This tests that ForLocalFunction correctly propagates the local variable type dictionary,
        // so FixInterpolationSyntax can detect that 'result' is truly dynamic.
        void LogResult()
        {
            Console.WriteLine( $"Method returned: {result}" );
        }

        LogResult();

        return result;
    }
}

// <target>
internal class Target
{
    [Log]
    public dynamic DoSomething( dynamic input )
    {
        return input;
    }
}
