// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @FormatOutput
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_SlotFields_TwoLevel;

// A trackable-style aspect that declares its own user slot. The template checks
// context.IsHandled(Slot) so base layers skip their body when a derived layer will run.
// The derived layer's override prologue emits base.OnConstructed(context.Descend(TheAspectSlots.Slot))
// to signal the base that its body should be skipped.
//
// Note: the slot is declared on a plain (non-compile-time) holder class rather than on the
// aspect type itself, because the template references it at run time and aspect types are
// [CompileTime], so their static fields cannot flow into run-time code directly.
public static class TheAspectSlots
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();
}

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var slotField = ((INamedType) builder.Target.Compilation.Factory
                .GetTypeByReflectionType( typeof(TheAspectSlots) ))
            .Fields.OfName( nameof(TheAspectSlots.Slot) ).Single();

        builder.AddInitializer(
            nameof(InitializerTemplate),
            InitializerKind.AfterLastInstanceConstructor,
            slotFields: new[] { slotField } );
    }

    [Template]
    private void InitializerTemplate( InitializationContext context )
    {
        if ( !context.IsHandled( TheAspectSlots.Slot ) )
        {
            Console.WriteLine( $"OnConstructed on {meta.Target.Type.Name}!" );
        }
    }
}

// <target>
[TheAspect]
public class BaseClass
{
    public BaseClass( int x )
    {
        _ = x;
    }
}

// <target>
public class DerivedClass : BaseClass
{
    public DerivedClass() : base( 0 )
    {
    }
}
