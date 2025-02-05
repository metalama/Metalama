using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.TypedConstant_;

public enum MyRunTimeEnum
{
    A,
    B
}

public class MyAttribute : Attribute
{
    private int _property;

    public MyRunTimeEnum Property
    {
        get => (MyRunTimeEnum)_property;
        set => _property = (int)value;
    }
}

public class MyAspect : MethodAspect
{
    public int Property { get; set; }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                namedArguments: new KeyValuePair<string, object?>[] { new( "Property", TypedConstant.Create( Property, typeof(MyRunTimeEnum) ) ) } ) );
    }
}

// <target>
internal class C
{
    [MyAspect( Property = (int)MyRunTimeEnum.B )]
    private void M() { }
}