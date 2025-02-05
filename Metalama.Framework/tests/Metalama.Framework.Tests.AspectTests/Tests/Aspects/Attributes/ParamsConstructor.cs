using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.ParamsConstructor;

[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
public class MyAttribute : Attribute
{
    public MyAttribute( params int[] x ) { }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Zero parameter.
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );

        // One parameter.
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object[] { 1 } ),
            whenExists: OverrideStrategy.New );

        // Many parameters.
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object[] { 1, 2 } ),
            whenExists: OverrideStrategy.New );

        // Passing an array.
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object[] { new int[] { 1, 2, 3 } } ),
            whenExists: OverrideStrategy.New );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}