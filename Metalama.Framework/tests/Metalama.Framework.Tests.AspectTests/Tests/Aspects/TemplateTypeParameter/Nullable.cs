using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.Nullable;

#pragma warning disable CS0219

public class GenerateMethodsAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach (var field in builder.Target.Fields)
        {
            builder.IntroduceMethod(
                nameof(Use),
                args: new { field = field, T = field.Type },
                buildMethod: m => m.Name = "Use" + field.Name );
        }
    }

    [Template]
    public void Use<[CompileTime] T>( IField field )
    {
        // not allowed by C#:
        // T? value = null;

        T? value = default;
    }
}

#pragma warning disable CS8618

// <target>
[GenerateMethods]
public class Target
{
    public string S;
    public string?[] ANS;
    public string? NS;
    public int I;
    public int? NI;
}