#if TEST_OPTIONS
// @Skipped(constructed generics not supported)
# endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.BaseType_GenericSubstituted;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass(
            "TestNestedType",
            buildType: t =>
            {
                var typeParameter = t.AddTypeParameter( "U" );
                t.BaseType = builder.Target.WithTypeArguments( typeParameter );
            } );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType<T> { }