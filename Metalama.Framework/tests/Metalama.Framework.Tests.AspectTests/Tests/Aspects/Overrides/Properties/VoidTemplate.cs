using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.VoidTemplate;

public class OverrideAttribute : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        base.BuildAspect( builder );

        builder.OverrideAccessors( nameof(OverrideMethod) );
    }

    [Template]
    public void OverrideMethod()
    {
        var value = meta.Proceed();
        meta.Return( value == null ? default : value );
    }
}

// <target>
internal class TargetClass
{
    [Override]
    private int P { get; set; }
}