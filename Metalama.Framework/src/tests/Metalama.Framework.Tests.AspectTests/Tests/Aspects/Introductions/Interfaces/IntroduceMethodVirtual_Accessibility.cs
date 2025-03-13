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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual_Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceMethod( nameof(TestPublic));
        @interface.IntroduceMethod( nameof(TestInternal));
        @interface.IntroduceMethod( nameof(TestProtected));
        @interface.IntroduceMethod( nameof(TestProtectedInternal));
        @interface.IntroduceMethod( nameof(TestPrivateProtected));
    }

    [Template]
    public virtual void TestPublic()
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    internal virtual void TestInternal()
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    protected virtual void TestProtected()
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    protected internal virtual void TestProtectedInternal()
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    private protected virtual void TestPrivateProtected()
    {
        Console.WriteLine("Implementation");
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif