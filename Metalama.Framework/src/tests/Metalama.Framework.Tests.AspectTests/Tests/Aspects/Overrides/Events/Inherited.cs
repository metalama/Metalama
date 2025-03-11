// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Aspects.Overrides.Events.Inherited;

public abstract class OverrideEventAttribute : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors( "add_Event", "remove_Event" );
    }

    [Template]
    public virtual event EventHandler? Event;
}

public class InheritedOverrideEventAttribute : OverrideEventAttribute
{
    public override event EventHandler? Event
    {
        add
        {
            Console.WriteLine( "Add accessor." );
        }
        remove
        {
            Console.WriteLine( "Remove accessor." );
        }
    }
}

internal class TargetClass
{
    // <target>
    [InheritedOverrideEvent]
    public event EventHandler? Event;
}