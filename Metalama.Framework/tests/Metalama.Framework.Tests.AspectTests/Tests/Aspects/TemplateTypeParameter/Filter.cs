using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.Filter;

#pragma warning disable CS8618, CS0169

internal class MyAspect : FieldAspect
{
    public override void BuildAspect( IAspectBuilder<IField> builder )
    {
        builder.AddContract( nameof(Filter), args: new { T = builder.Target.Type } );
    }

    [Template]
    public void Filter<[CompileTime] T>( T? value )
    {
        Console.WriteLine( typeof(T).Name );
    }
}

// <target>
internal class Target
{
    [MyAspect]
    private string? q;
}