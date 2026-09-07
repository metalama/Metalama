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

/*
 * C# 15 changes three declaration kinds, and the compiler reports them from three different symbols. The static
 * method with a body comes from SourceMemberMethodSymbol and IntroduceMethodStatic covers it. The static field comes
 * from SourceMemberFieldSymbol, and a static automatic property is the way an aspect reaches it, because the compiler
 * generates the backing field. LAMA0534 refuses a field introduced directly into an interface, which this test does
 * not change. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroducePropertyStaticAuto;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest" );
        @interface.IntroduceProperty( nameof(TestProperty) );
    }

    [Template]
    public static int TestProperty { get; set; }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
