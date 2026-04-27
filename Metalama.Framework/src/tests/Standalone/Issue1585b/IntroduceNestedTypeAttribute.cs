using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Repro;

public sealed class IntroduceNestedTypeAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass(
            "Nested",
            buildType: t => t.Accessibility = Metalama.Framework.Code.Accessibility.Public );
    }
}
