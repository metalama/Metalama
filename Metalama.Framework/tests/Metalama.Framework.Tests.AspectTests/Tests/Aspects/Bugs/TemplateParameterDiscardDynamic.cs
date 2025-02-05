using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Bugs.TemplateParameterDiscardDynamic;

internal class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    private void Template( dynamic arg )
    {
        _ = arg;
    }
}

// <target>
internal class Program
{
    [Aspect]
    private void M( int arg ) { }
}