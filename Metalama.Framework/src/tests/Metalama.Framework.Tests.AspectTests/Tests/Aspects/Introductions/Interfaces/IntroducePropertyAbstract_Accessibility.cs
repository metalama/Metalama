// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0626

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyAbstract_Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceProperty( nameof(TestPublic));
        @interface.IntroduceProperty( nameof(TestInternal));
        @interface.IntroduceProperty( nameof(TestProtected));
        @interface.IntroduceProperty( nameof(TestProtectedInternal));
        @interface.IntroduceProperty( nameof(TestPrivateProtected));
    }

    [Template]
    public extern int TestPublic{ get; set; }

    [Template]
    internal extern int TestInternal { get; set; }

    [Template]
    protected extern int TestProtected { get; set; }

    [Template]
    protected internal extern int TestProtectedInternal { get; set; }

    [Template]
    private protected extern int TestPrivateProtected { get; set; }
}

// <target>
[IntroductionAttribute]
public class TargetType { } 
#endif