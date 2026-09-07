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
 * C# 15 makes a non-virtual static member with a body legal in an interface on a runtime that does not support
 * default interface implementations. This test introduces such a method into an interface introduced by the aspect
 * and asserts the transformed code on both legs of the test project, that is on net48 and on net10.0. Before C# 15
 * the net48 leg reported CS8652 for the introduced method. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroduceMethodStatic;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest" );
        @interface.IntroduceMethod( nameof(TestMethod) );
    }

    [Template]
    public static void TestMethod()
    {
        Console.WriteLine( "Default" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
