using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddWithNamedParameterOfNamedType;

public class MyAttribute : Attribute
{
    public Type? Property { get; set; }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                namedArguments: new KeyValuePair<string, object?>[] { new( "Property", TypeFactory.GetType( typeof(C) ) ) } ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}