using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AsMethodParameter;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass(
            "IntroducedNestedType",
            buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        var existingNested = builder.Target.Types.Single();

        builder.IntroduceMethod(
            nameof(MethodTemplate),
            buildMethod: b =>
            {
                b.Name = "MethodWithIntroduced";
                b.AddParameter( "p", result.Declaration );
            } );

        builder.IntroduceMethod(
            nameof(MethodTemplate),
            buildMethod: b =>
            {
                b.Name = "MethodWithExisting";
                b.AddParameter( "p", existingNested );
            } );
    }

    [Template]
    public void MethodTemplate() { }
}

// <target>
[IntroductionAttribute]
public class TargetType
{
    public class ExistingNestedType { }
}