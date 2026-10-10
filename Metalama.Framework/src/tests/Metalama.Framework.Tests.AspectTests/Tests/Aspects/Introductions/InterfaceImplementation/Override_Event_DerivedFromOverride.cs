// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_Event_DerivedFromOverride;

// Tests that an inherited non-sealed override event can be overridden (#2148).

public interface IInterface
{
    event EventHandler Event;
}

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IInterface), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public event EventHandler Event
    {
        add
        {
            Console.WriteLine( "TheAspect.Event.add" );
            meta.Proceed();
        }

        remove
        {
            Console.WriteLine( "TheAspect.Event.remove" );
            meta.Proceed();
        }
    }
}

internal class A : IInterface
{
    public virtual event EventHandler? Event;
}

internal class B : A
{
    public override event EventHandler? Event;
}

// <target>
[TheAspect]
internal class C : B { }
