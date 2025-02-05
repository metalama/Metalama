using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.Abstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass( "SealedType", buildType: t => { t.IsAbstract = true; } );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }