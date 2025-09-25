// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable IDE0052

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke;

public class OverrideAttribute : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors( invokeTemplate: nameof( InvokeEventTemplate ));
    }

    [Template]
    public void InvokeEventTemplate()
    {
        Console.WriteLine( "Invoke" );
        meta.Proceed();
    }
}

// <target>
internal class TargetClass 
{
    private EventHandler? _handler;

    [Override]
    public event EventHandler Event
    {
        add
        {
            this._handler = value;
        }

        remove
        {
            this._handler = null;
        }
    }
}