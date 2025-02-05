using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddWithParameter;

public class MyAttribute : Attribute
{
    public MyAttribute( int param ) { }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object?[] { 42 } ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}