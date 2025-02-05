

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.Conflict_WithIntroduced;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        _ = builder.With( builder.Target.ContainingNamespace ).WithChildNamespace( "TestNamespace" );
        var ns = builder.With( builder.Target.ContainingNamespace ).WithChildNamespace( "TestNamespace" );
        ns.IntroduceClass( "TestNestedType" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }