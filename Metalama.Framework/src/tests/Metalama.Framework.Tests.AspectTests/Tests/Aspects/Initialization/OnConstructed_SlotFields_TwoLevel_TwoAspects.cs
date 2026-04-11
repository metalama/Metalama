// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_SlotFields_TwoLevel_TwoAspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_SlotFields_TwoLevel_TwoAspects;

// Two different aspects, each declaring its own user slot, are applied together to a base class
// that is also inherited. The derived override must call `base.OnConstructed(...)` with BOTH
// slots combined via the `|` operator, so that each aspect's guard in the base body (which
// checks `context.IsHandled(ItsOwnSlot)`) correctly skips when the derived layer will re-run it.
public static class Slots
{
    public static readonly InitializationSlot FirstSlot = InitializationSlot.Allocate();
    public static readonly InitializationSlot SecondSlot = InitializationSlot.Allocate();
}

[Inheritable]
public class FirstAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var slotField = ((INamedType) builder.Target.Compilation.Factory
                .GetTypeByReflectionType( typeof(Slots) ))
            .Fields.OfName( nameof(Slots.FirstSlot) ).Single();

        builder.AddInitializer(
            nameof(Template),
            InitializerKind.AfterLastInstanceConstructor,
            slotFields: new[] { slotField } );
    }

    [Template]
    private void Template( InitializationContext context )
    {
        if ( !context.IsHandled( Slots.FirstSlot ) )
        {
            Console.WriteLine( $"First on {meta.Target.Type.Name}" );
        }
    }
}

[Inheritable]
public class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var slotField = ((INamedType) builder.Target.Compilation.Factory
                .GetTypeByReflectionType( typeof(Slots) ))
            .Fields.OfName( nameof(Slots.SecondSlot) ).Single();

        builder.AddInitializer(
            nameof(Template),
            InitializerKind.AfterLastInstanceConstructor,
            slotFields: new[] { slotField } );
    }

    [Template]
    private void Template( InitializationContext context )
    {
        if ( !context.IsHandled( Slots.SecondSlot ) )
        {
            Console.WriteLine( $"Second on {meta.Target.Type.Name}" );
        }
    }
}

// <target>
[FirstAspect]
[SecondAspect]
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
