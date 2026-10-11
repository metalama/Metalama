// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_PropertyAndEvent_DerivedNonVirtual;

/*
 * Tests that an error is emitted when an inherited non-virtual property or event would have to be overridden with OverrideStrategy.Override (#2145).
 */

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IInterface), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public int Property
    {
        get => meta.Proceed();
        set => meta.Proceed();
    }

    [InterfaceMember]
    public event EventHandler? Event
    {
        add => meta.Proceed();
        remove => meta.Proceed();
    }
}

public interface IInterface
{
    int Property { get; set; }

    event EventHandler? Event;
}

internal class C : IInterface
{
    public int Property { get; set; }

    public event EventHandler? Event;
}

// <target>
[TheAspect]
internal class D : C { }
