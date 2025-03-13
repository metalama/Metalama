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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual_Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceEvent( nameof(TestPublic));
        @interface.IntroduceEvent( nameof(TestInternal));
        @interface.IntroduceEvent( nameof(TestProtected));
        @interface.IntroduceEvent( nameof(TestProtectedInternal));
        @interface.IntroduceEvent( nameof(TestPrivateProtected));
    }

    [Template]
    public event EventHandler TestPublic
    {
        add
        {
            Console.WriteLine("Default");
        }
        remove
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    internal event EventHandler TestInternal
    {
        add
        {
            Console.WriteLine("Default");
        }
        remove
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    protected event EventHandler TestProtected
    {
        add
        {
            Console.WriteLine("Default");
        }
        remove
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    protected internal event EventHandler TestProtectedInternal
    {
        add
        {
            Console.WriteLine("Default");
        }
        remove
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    private protected event EventHandler TestPrivateProtected
    {
        add
        {
            Console.WriteLine("Default");
        }
        remove
        {
            Console.WriteLine("Default");
        }
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif