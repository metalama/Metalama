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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual_Accessibility;

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
    public int TestPublic
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    internal int TestInternal
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    protected int TestProtected
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    protected internal int TestProtectedInternal
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    private protected int TestPrivateProtected
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif