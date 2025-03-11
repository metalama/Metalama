// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;
using System.Runtime.CompilerServices;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.CallerMemberAttribute;
using System;

[assembly: AspectOrder(AspectOrderDirection.CompileTime, typeof(OverrideAspect), typeof(InvokerAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.CallerMemberAttribute;

/*
 * Tests that invokers targeting a non-overridden event that calls a method with [CallerMemberAttribute] are not rewritten.
 */

public class InvokerAspect : TypeAspect
{
    [Introduce]
    public void CallFoo()
    {
        foreach (var @event in meta.Target.Type.Events)
        {
            @event.Add(null);
            @event.With(InvokerOptions.Base).Add(null);
            @event.With(InvokerOptions.Current).Add(null);
            @event.With(InvokerOptions.Final).Add(null);
        }

        meta.Proceed();
    }
}

public class OverrideAspect : OverrideEventAspect
{
    public override void OverrideAdd(dynamic value)
    {
        if (meta.Target.Event.Name == nameof(TargetClass.TestOverriddenEvent))
        {
            meta.Proceed();
        }
        else
        {
            meta.Proceed();
            meta.Proceed();
        }
    }

    public override void OverrideRemove(dynamic value)
    {
        meta.Proceed();
    }
}

public class OtherClass
{
    public static void Foo([CallerMemberName] string? callerMemberName = null )
    {
    }
}

// <target>
[InvokerAspect]
public class TargetClass
{
    public event EventHandler TestEvent
    {
        add { }
        remove { OtherClass.Foo(); }
    }

    [OverrideAspect]
    public event EventHandler TestOverriddenEvent
    {
        add { }
        remove { OtherClass.Foo(); }
    }

    [OverrideAspect]
    public event EventHandler TestOverriddenNonInlinedEvent
    {
        add { }
        remove { OtherClass.Foo(); }
    }
}
