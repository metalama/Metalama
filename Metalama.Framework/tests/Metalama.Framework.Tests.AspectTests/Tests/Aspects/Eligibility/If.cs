using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.If;

public class MyBaseClass<T> { }

public class MyAttribute : Attribute
{
    public MyAttribute( params string[] args ) { }
}

public class MyAspect : TypeAspect
{
    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.MustSatisfy( t => t.Attributes.OfAttributeType( typeof(MyAttribute) ).Any(), x => $"{x} must have an attribute of type MyAttribute" );

        builder.If( t => t.Attributes.OfAttributeType( typeof(MyAttribute) ).Any() )
            .MustSatisfy(
                t => t.Attributes.OfAttributeType( typeof(MyAttribute) ).First().ConstructorArguments.Length >= 3,
                x => $"The MyAttribute must have at least 3 arguments" );
    }
}

[MyAttribute]
[MyAspect]
public class Test { }