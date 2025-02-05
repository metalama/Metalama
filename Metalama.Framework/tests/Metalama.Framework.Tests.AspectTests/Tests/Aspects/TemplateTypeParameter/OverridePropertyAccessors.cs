using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.OverridePropertyAccessors;

public class Aspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        base.BuildAspect( builder );

        builder.OverrideAccessors( getTemplate: nameof(GetTemplate), setTemplate: nameof(SetTemplate), args: new { T = builder.Target.Type } );
    }

    [Template]
    private T GetTemplate<[CompileTime] T>()
    {
        Console.WriteLine( typeof(T) );

        return (T)meta.Proceed()!;
    }

    [Template]
    private void SetTemplate<[CompileTime] T>( T value )
    {
        Console.WriteLine( typeof(T) );
        meta.Proceed();
    }
}

// <target>
public class Target
{
    [Aspect]
    public int P { get; set; }
}