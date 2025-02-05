using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug35456;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var baseNested = builder.Target.BaseType.Types.SingleOrDefault();

        var nestedType = builder.IntroduceClass(
            "Builder",
            whenExists: OverrideStrategy.New,
            buildType: t => t.Accessibility = Accessibility.Public );

        nestedType.IntroduceMethod(
            nameof(Template),
            whenExists: OverrideStrategy.Override,
            buildMethod: m =>
            {
                if (baseNested != null)
                {
                    m.ReturnType = baseNested;
                }
            } );
    }

    [Template]
    private dynamic? Template() => null!;
}

// <target>
[TheAspect]
internal class C { }

// <target>
internal class D : C { }