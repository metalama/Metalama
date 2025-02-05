using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0169 // The field 'Target.i' is never used

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.AbstractPropertyTemplate;

public abstract class AbstractAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.Override( nameof(OverrideProperty) );
    }

    [Template]
    public abstract dynamic? OverrideProperty { get; set; }
}

public class DerivedAspect : AbstractAspect
{
    public override dynamic? OverrideProperty
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}

// <target>
internal class Target
{
    [DerivedAspect]
    private int i;
}