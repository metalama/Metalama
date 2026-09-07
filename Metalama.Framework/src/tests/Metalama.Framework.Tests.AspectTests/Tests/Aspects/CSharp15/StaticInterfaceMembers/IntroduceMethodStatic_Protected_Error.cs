// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
// @TargetFrameworks(net48)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

/*
 * C# 15 does not legalise every accessibility. ReportLackOfRuntimeSupportForStaticMembersInInterfaces still reports
 * ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember, that is CS8707, for a protected, a protected internal
 * and a private protected member. The test is requested on the net48 leg only, because the diagnostic depends on the
 * runtime and the net10.0 leg accepts the same declarations. On the net10.0 leg the test is skipped, and the reason
 * names the target framework. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroduceMethodStatic_Protected_Error;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest" );
        @interface.IntroduceMethod( nameof(TestProtected) );
        @interface.IntroduceMethod( nameof(TestProtectedInternal) );
        @interface.IntroduceMethod( nameof(TestPrivateProtected) );
    }

    [Template]
    protected static void TestProtected()
    {
        Console.WriteLine( "Protected" );
    }

    [Template]
    protected internal static void TestProtectedInternal()
    {
        Console.WriteLine( "ProtectedInternal" );
    }

    [Template]
    private protected static void TestPrivateProtected()
    {
        Console.WriteLine( "PrivateProtected" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
