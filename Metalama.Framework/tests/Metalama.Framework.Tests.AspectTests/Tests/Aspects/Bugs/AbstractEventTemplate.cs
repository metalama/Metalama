// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.AbstractEventTemplate;

public abstract class AbstractAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEvent( nameof(IntroducedEvent) );
    }

    [Template]
    public abstract event EventHandler<dynamic> IntroducedEvent;
}

public class DerivedAspect : AbstractAspect
{
    public override event EventHandler<dynamic> IntroducedEvent
    {
        add => throw new NotSupportedException();
        remove => throw new NotSupportedException();
    }
}

// <target>
[DerivedAspect]
internal class Target { }