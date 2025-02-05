using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AsProperty;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass( "IntroducedNestedType", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );
        var existingNested = builder.Target.Types.Single();

        builder.IntroduceProperty(
            nameof(PropertyTemplate),
            buildProperty: b =>
            {
                b.Name = "PropertyWithIntroduced";
                b.Type = result.Declaration;
            } );

        builder.IntroduceProperty(
            nameof(PropertyTemplate),
            buildProperty: b =>
            {
                b.Name = "PropertyWithExisting";
                b.Type = existingNested;
            } );
    }

    [Template]
    public object? PropertyTemplate { get; set; }
}

#pragma warning disable CS8618

// <target>
[IntroductionAttribute]
public class TargetType
{
    public class ExistingNestedType { }
}