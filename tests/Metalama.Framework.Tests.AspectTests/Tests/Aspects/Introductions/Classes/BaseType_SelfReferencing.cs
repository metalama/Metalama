#if TEST_OPTIONS
// @Skipped(constructed generics not supported)
# endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.BaseType_SelfReferencing;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass(
            "TestNestedType",
            buildType: t => { t.BaseType = builder.Target.WithTypeArguments( t ); } );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType<T> { }