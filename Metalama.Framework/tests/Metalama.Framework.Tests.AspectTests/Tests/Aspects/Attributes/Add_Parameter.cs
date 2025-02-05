using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Parameter;

public class MyAttribute : Attribute { }

[Inheritable]
public class MyAspect : ParameterAspect
{
    public override void BuildAspect( IAspectBuilder<IParameter> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
    }
}

// <target>
internal class C
{
    private void M( [MyAspect] int p ) { }
}