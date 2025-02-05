using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.CompileTimeTypeParameterMetaCompileTime;

internal class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(Method), args: new { T = typeof(int) } );
    }

    [Template]
    public void Method<[CompileTime] T>()
    {
        _ = meta.CompileTime( default(T) );
    }
}

// <target>
[MyAspect]
internal class Target { }