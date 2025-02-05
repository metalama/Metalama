using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.InvalidFieldModifier;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceField( "field", typeof(int), buildField: field => field.Writeability = Writeability.InitOnly );
    }
}

// <target>
[Aspect]
internal class TargetClass { }