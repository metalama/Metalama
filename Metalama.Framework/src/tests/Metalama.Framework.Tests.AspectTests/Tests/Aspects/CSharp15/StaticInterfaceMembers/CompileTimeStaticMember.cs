// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @TargetFrameworks(net48;net10.0)
#endif

using Metalama.Framework.Aspects;
using System;

/*
 * The compile-time compilation is always built against the netstandard2.0 reference set, which has no runtime support
 * for default interface implementations, so a static member with a body in a compile-time interface is the second
 * declaration that C# 15 legalises for Metalama.
 *
 * The preview language version requested by this test reaches the compile-time compilation, so the expected output is
 * the transformed code. The compile-time language version comes from ILanguageVersionProvider, which returned C# 14
 * whatever the project asked for until issue #1979, and the declaration below was then refused on both legs of the
 * test project.
 *
 * This file is removed from the compilation of the test project by Metalama.Framework.Tests.AspectTests.csproj,
 * because the project itself is pinned to language version 14 and its net48 leg would report CS8652 for the
 * declaration below. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_CompileTimeStaticMember;

[CompileTime]
internal interface IFormatter
{
    static string Format( int value ) => $"[{value}]";
}

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( IFormatter.Format( 42 ) );

        return meta.Proceed();
    }
}

// <target>
internal class TargetType
{
    [TheAspect]
    public void Method() { }
}
