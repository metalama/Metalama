// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_SlotFields;

public class TheAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var slotField = ((INamedType) builder.Target.Compilation.Factory
                .GetTypeByReflectionType( typeof(TheAspect) ))
            .Fields.OfName( nameof(Slot) ).Single();

        builder.AddInitializer(
            nameof(InitializerTemplate),
            InitializerKind.AfterLastInstanceConstructor,
            slotFields: new[] { slotField } );
    }

    [Template]
    private void InitializerTemplate()
    {
        // Note: in Phase 1, slotFields is accepted by the API but the slot-coordination
        // code path is not yet implemented — Phase 3 will emit `if (!context.IsHandledBy(Slot))`
        // guards around the body. For now we just verify that passing slotFields does not
        // crash and that the transformation still emits the expected code.
        Console.WriteLine( "OnConstructed!" );
    }
}

// <target>
[TheAspect]
public class TargetCode
{
    public TargetCode( int value )
    {
        _ = value;
    }
}
