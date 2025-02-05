using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.Issue30361;

internal class NormalizeAttribute : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.Override( nameof(OverrideProperty) );
    }

    [Template]
    private string OverrideProperty
    {
        set => meta.Target.FieldOrProperty.Value = value?.Trim().ToLowerInvariant();
    }
}

// <target>
internal class Foo
{
    [Normalize]
    public string? Property { get; set; }
}