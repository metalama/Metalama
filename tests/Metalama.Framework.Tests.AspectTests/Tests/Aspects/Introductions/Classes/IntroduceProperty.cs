using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.IntroduceProperty;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass( "TestNestedType" );

        result.IntroduceProperty( nameof(Property) );
    }

    [Template]
    public int Property { get; set; }
}

// <target>
[IntroductionAttribute]
public class TargetType { }