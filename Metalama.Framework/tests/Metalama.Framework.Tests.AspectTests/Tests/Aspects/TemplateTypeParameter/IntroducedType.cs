using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.IntroducedType;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.IntroduceClass( "IntroducedType" ).Declaration;

        builder.IntroduceMethod(
            nameof(Method),
            args: new { T = type },
            buildMethod: b => { b.Name = "FromBaseCompilation"; } );

        builder.IntroduceMethod(
            nameof(Method),
            args: new { T = type },
            buildMethod: b => { b.Name = "FromMutableCompilation"; } );
    }

    [Template]
    private void Method<[CompileTime] T>()
    {
        Console.WriteLine( typeof(T).Name );
    }
}

// <target>
[Aspect]
public class Target { }