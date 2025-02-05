using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Constructor;

public class MyAttribute : Attribute { }

[Inheritable]
public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private C() { }
}