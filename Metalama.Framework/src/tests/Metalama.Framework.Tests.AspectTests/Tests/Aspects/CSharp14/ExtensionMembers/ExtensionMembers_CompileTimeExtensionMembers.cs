// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_CompileTimeExtensionMembers;

/*
 * An extension block declared in a compile-time type matches no arm of the member switch of
 * TransformCompileTimeType and is copied to the compile-time compilation by the base rewriter. Its members are
 * ordinary compile-time members and a template can call them. This test pins that behaviour. (#1932)
 */

[CompileTime]
internal static class Helpers
{
    extension( int value )
    {
        public int Twice => value * 2;

        public int Triple() => value * 3;
    }
}

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var compileTimeValue = 21.Twice + 7.Triple();
        Console.WriteLine( compileTimeValue );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    private void M() { }
}
