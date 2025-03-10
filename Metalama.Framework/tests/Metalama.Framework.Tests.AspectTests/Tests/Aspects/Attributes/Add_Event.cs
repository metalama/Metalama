// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event;

#pragma warning disable CS0169, CS0067

public class MyAttribute : Attribute { }

public class MyAspect : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );

        if (builder.Target.AddMethod != null)
        {
            builder.With( builder.Target.AddMethod ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
        }

        if (builder.Target.RemoveMethod != null)
        {
            builder.With( builder.Target.RemoveMethod ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
        }
    }
}

#pragma warning disable CS8618

// <target>
internal class C
{
    [MyAspect]
    private event Action Event1, Event2;

    [MyAspect]
    private event Action Event3;

    [MyAspect]
    private event Action Event4
    {
        add { }
        remove { }
    }
}