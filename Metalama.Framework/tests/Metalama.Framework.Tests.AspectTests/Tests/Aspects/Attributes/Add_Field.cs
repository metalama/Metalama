using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Field;

#pragma warning disable CS8618, CS0169

public class MyAttribute : Attribute { }

public class MyAspect : FieldAspect
{
    public override void BuildAspect( IAspectBuilder<IField> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private int Field1, Field2;

    [MyAspect]
    private int Field3;
}

// <target>
internal enum E
{
    [MyAspect]
    A
}