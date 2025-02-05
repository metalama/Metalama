using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.Empty;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Compilation.GlobalNamespace ).WithChildNamespace( "Implementations" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }