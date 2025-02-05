using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AddTypeParameter;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass( "TestNestedType", buildType: b => { b.AddTypeParameter( "T" ); } );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }