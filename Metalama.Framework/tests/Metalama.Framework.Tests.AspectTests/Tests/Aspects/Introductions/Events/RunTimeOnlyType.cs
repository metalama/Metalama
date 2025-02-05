#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.RunTimeOnlyType;

public class Aspect : TypeAspect
{
    [Introduce]
    internal event EventHandler<RunTimeClass>? FieldLikeEvent;

    [Introduce]
    internal event EventHandler<RunTimeClass> Event
    {
        add { }
        remove { }
    }
}

// <target>
[Aspect]
public class RunTimeClass { }