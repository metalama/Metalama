using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.BaseType_Abstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass( "TestNestedType", buildType: t => { t.BaseType = builder.Target; } );

        result.IntroduceProperty( nameof(Property), whenExists: OverrideStrategy.Override );
        result.IntroduceMethod( nameof(Method), whenExists: OverrideStrategy.Override );
    }

    [Template]
    public int Property { get; set; }

    [Template]
    public void Method() { }
}

// <target>
[IntroductionAttribute]
public abstract class TargetType
{
    public abstract int Property { get; set; }

    public abstract void Method();
}