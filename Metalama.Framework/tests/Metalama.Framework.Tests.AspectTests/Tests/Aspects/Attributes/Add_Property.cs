using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Property;

public class MyAttribute : Attribute { }

public class MyAspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );

        if (builder.Target.GetMethod != null)
        {
            builder.With( builder.Target.GetMethod ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
        }

        if (builder.Target.SetMethod != null)
        {
            builder.With( builder.Target.SetMethod ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
        }
    }
}

// <target>
internal class C
{
    [MyAspect]
    private int Property1 { get; set; }
}