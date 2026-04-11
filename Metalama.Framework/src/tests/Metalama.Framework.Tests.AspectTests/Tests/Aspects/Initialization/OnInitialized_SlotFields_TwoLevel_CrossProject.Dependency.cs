// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_SlotFields_TwoLevel_CrossProject;

// Cross-project variant of OnInitialized_SlotFields_TwoLevel. TheAspect, TheAspectSlots and
// BaseClass live in the dependency project; the derived class lives in the main project and
// picks up the aspect via [Inheritable] propagation.
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

[TheAspect]
public class BaseClass
{
}
