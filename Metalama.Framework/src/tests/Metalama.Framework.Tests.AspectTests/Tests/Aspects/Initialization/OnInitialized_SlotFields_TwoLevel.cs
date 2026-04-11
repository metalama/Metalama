// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_SlotFields_TwoLevel;

// Two-level inheritance using AfterObjectInitializer (Initialize method) with a user slot.
// The aspect's template checks context.IsHandled(Slot) so base Initialize skips its body
// when the derived layer will run. The derived Initialize's base call passes
// context.Descend(TheAspectSlots.Slot) to signal the base.
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
            InitializerKind.AfterObjectInitializer,
            slotFields: new[] { slotField } );
    }

    [Template]
    private void InitializerTemplate( InitializationContext context )
    {
        if ( !context.IsHandled( TheAspectSlots.Slot ) )
        {
            Console.WriteLine( $"Initialized {meta.Target.Type.Name}" );
        }
    }
}

// <target>
[TheAspect]
public class BaseClass
{
}

// <target>
public class DerivedClass : BaseClass
{
}
