using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.GenericType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.AddParameter( "p", builder.Target.TypeParameters[0] ); } );

        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.IsStatic = true; } );
    }

    [Template]
    public void Template() { }
}

// <target>
[Introduction]
internal class TargetClass<T> { }