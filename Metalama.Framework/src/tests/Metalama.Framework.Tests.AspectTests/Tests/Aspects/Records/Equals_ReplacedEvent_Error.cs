// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_ReplacedEvent_Error;

/// <summary>
/// The aspect overrides the accessors of the field-like event with templates that do not call the original implementation,
/// so the event keeps no handler and the linker emits no backing field for it. The materialized <c>Equals</c> has nothing
/// to read, which the linker reports as <c>LAMA0655</c>.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );

        builder.With( builder.Target.Events.Single( e => e.Name == "Changed" ) )
            .OverrideAccessors( nameof(AccessorTemplate), nameof(AccessorTemplate) );
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        return meta.Proceed();
    }

    [Template]
    public void AccessorTemplate()
    {
        Console.WriteLine( "The aspect discards the handler." );
    }
}

// <target>
[Override]
internal record Target
{
    public int X;

    // Nothing reads the original implementation of the event, so the linker emits no backing field for it.
    public event EventHandler? Changed;
}
