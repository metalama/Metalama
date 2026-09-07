// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
// @TargetFrameworks(net48;net10.0)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

/*
 * The scenario an aspect user meets: the interface is declared in the source and the aspect introduces a non-virtual
 * static method into it. The interface itself carries no static member in the source, so this file compiles at the
 * pinned language version of the test project on both legs, and only the transformed code needs C# 15.
 * See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroduceMethodStatic_SourceInterface;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(TestMethod) );
    }

    [Template]
    public static void TestMethod()
    {
        Console.WriteLine( "Default" );
    }
}

// <target>
[IntroductionAttribute]
public interface ITargetInterface
{
    void ExistingMethod();
}
