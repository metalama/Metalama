using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.GenericReturnType;

public class Override : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template), args: new { T = builder.Target.ReturnType } );
    }

    [Template]
    private T Template<[CompileTime] T>()
    {
        T LocalFunction()
        {
            return meta.Proceed()!;
        }

        return LocalFunction();
    }
}

// <target>
internal class TargetClass
{
    [Override]
    private int Method() => 5;
}