using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.MustBe;

public class MyBaseClass<T> { }

public class MyAspect : TypeAspect
{
    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.MustBeConvertibleTo( typeof(MyBaseClass<>), ConversionKind.TypeDefinition );
    }
}

[MyAspect]
public class Test { }