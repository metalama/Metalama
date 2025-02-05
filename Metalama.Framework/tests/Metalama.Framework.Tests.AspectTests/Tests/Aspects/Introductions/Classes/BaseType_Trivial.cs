using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.BaseType_Trivial;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass( "TestNestedType", buildType: t => { t.BaseType = builder.Target; } );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }