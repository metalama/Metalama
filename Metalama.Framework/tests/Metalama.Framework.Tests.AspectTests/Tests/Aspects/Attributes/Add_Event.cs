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