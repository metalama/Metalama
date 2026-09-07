// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @TargetFrameworks(net48;net10.0)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

/*
 * ReportLackOfRuntimeSupportForStaticMembersInInterfaces treats every accessibility other than protected, protected
 * internal and private protected alike, so a private static method and an internal one are accepted on the net48 leg
 * exactly as a public one is. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroduceMethodStatic_Private;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest" );
        @interface.IntroduceMethod( nameof(TestPrivate) );
        @interface.IntroduceMethod( nameof(TestInternal) );
    }

    [Template]
    private static void TestPrivate()
    {
        Console.WriteLine( "Private" );
    }

    [Template]
    internal static void TestInternal()
    {
        Console.WriteLine( "Internal" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
