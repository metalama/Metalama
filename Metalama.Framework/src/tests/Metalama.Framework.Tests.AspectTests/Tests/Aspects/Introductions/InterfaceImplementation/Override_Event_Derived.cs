// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_Event_Derived;

/*
 * Tests that an inherited virtual event is overridden when the interface is implemented with OverrideStrategy.Override (#2145).
 */

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IInterface), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public event EventHandler? Event
    {
        add
        {
            Console.WriteLine( "TheAspect.Event" );
            meta.Proceed();
        }

        remove
        {
            Console.WriteLine( "TheAspect.Event" );
            meta.Proceed();
        }
    }
}

public interface IInterface
{
    event EventHandler? Event;
}

internal class C : IInterface
{
    public virtual event EventHandler? Event
    {
        add { }
        remove { }
    }
}

// <target>
[TheAspect]
internal class D : C { }
