// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.PragmaWarningAroundMethodSignature;

/*
 * Tests that pragma warning directives around method signatures are preserved correctly
 * when the method is overridden by an aspect. Issue #838.
 */

public class OverrideAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Override" );

        return meta.Proceed();
    }
}

// <target>
internal class TargetClass
{
    // Pragma warning before the method signature
#pragma warning disable CA1822
    [Override]
    public void MethodWithPragmaBeforeSignature()
    {
        Console.WriteLine( "Original" );
    }
#pragma warning restore CA1822

    // Pragma warning between attribute and method signature
    [Override]
#pragma warning disable CA1822
    public void MethodWithPragmaBetweenAttributeAndSignature()
    {
        Console.WriteLine( "Original" );
    }
#pragma warning restore CA1822

    // Pragma warning after parameter list, before body
    [Override]
    public void MethodWithPragmaAfterParameters()
#pragma warning disable CA1822
    {
        Console.WriteLine( "Original" );
    }
#pragma warning restore CA1822

    // Pragma warning on return type line
#pragma warning disable CA1822
    [Override]
    public
#pragma warning restore CA1822
        void MethodWithPragmaOnReturnType()
    {
        Console.WriteLine( "Original" );
    }
}
