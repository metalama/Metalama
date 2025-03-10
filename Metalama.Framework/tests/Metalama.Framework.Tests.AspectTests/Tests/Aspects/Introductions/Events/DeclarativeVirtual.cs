// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.DeclarativeVirtual;

public abstract class IntroductionAttribute : TypeAspect
{
    [Introduce( IsVirtual = false )]
    public virtual event EventHandler VirtualOverriddenIntroduction
    {
        add
        {
            Console.WriteLine( "Base template (wrong)." );
        }
        remove
        {
            Console.WriteLine( "Base template (wrong)." );
        }
    }

    [Introduce( IsVirtual = false )]
    public virtual event EventHandler VirtualIntroduction
    {
        add
        {
            Console.WriteLine( "Base template (expected)." );
        }
        remove
        {
            Console.WriteLine( "Base template (expected)." );
        }
    }
}

public class InheritedIntroductionAttribute : IntroductionAttribute
{
    public override event EventHandler VirtualOverriddenIntroduction
    {
        add
        {
            Console.WriteLine( "Base template (expected)." );
        }
        remove
        {
            Console.WriteLine( "Base template (expected)." );
        }
    }
}

// <target>
[InheritedIntroduction]
internal class TargetClass { }