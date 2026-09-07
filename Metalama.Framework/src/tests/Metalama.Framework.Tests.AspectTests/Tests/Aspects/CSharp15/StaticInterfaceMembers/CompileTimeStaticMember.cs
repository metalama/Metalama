// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
// @TargetFrameworks(net48;net10.0)
#endif

using Metalama.Framework.Aspects;
using System;

/*
 * The compile-time compilation is always built against the netstandard2.0 reference set, which has no runtime support
 * for default interface implementations, so a static member with a body in a compile-time interface is the second
 * declaration that C# 15 legalises for Metalama.
 *
 * The expected output records CS8652 and not the transformed code, because the preview language version requested by
 * this test does not reach the compile-time compilation. The compile-time language version comes from
 * ILanguageVersionProvider, which returns C# 14 whatever the project asks for, so the declaration below is refused on
 * both legs of the test project. Issue #1979 covers that, and this test turns into a positive one when it is fixed.
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
