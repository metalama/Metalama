using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.OverridePropertyAccessors;

public class Aspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        base.BuildAspect( builder );

        builder.OverrideAccessors(
            getTemplate: nameof(GetTemplate),
            setTemplate: nameof(SetTemplate),
            args: new { a = 5, t = builder.Target.Type } );
    }

    [Template]
    private dynamic? GetTemplate( [CompileTime] int a, INamedType t )
    {
        Console.WriteLine( a );
        Console.WriteLine( t.ToDisplayString() );

        return meta.Proceed();
    }

    [Template]
    private void SetTemplate( [CompileTime] int a, dynamic value, INamedType t )
    {
        Console.WriteLine( a );
        Console.WriteLine( t.ToDisplayString() );
        meta.Proceed();
    }
}

// <target>
public class Target
{
    [Aspect]
    public int P { get; set; }
}