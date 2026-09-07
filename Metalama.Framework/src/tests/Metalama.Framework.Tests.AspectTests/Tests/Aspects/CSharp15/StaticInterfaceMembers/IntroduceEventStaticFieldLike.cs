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
 * The third declaration kind that C# 15 changes is the static field-like event, which the compiler reports from
 * SourceFieldLikeEventSymbol. IntroduceEventStatic covers the event whose accessors have a body; this test covers the
 * field-like form, whose backing field is the declaration the feature legalises. See issue #1938.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.StaticInterfaceMembers_IntroduceEventStaticFieldLike;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest" );
        @interface.IntroduceEvent( nameof(TestEvent) );
    }

    [Template]
    public static event EventHandler? TestEvent;
}

// <target>
[IntroductionAttribute]
public class TargetType { }
