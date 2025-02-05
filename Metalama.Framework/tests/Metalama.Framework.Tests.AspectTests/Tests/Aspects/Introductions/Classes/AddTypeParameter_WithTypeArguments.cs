#if TEST_OPTIONS
// @Skipped(constructed generics are not supported)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AddTypeParameter_WithTypeArguments;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass(
            "TestNestedType",
            buildType: b =>
            {
                b.AddTypeParameter( "T" );
                b.Accessibility = Code.Accessibility.Public;
            } );

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: b => { b.AddParameter( "p", result.Declaration.WithTypeArguments( TypeFactory.GetType( SpecialType.Object ) ) ); } );
    }

    [Template]
    public void Template() { }
}

// <target>
[IntroductionAttribute]
public class TargetType { }