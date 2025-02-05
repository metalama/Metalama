using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.Filter;

#pragma warning disable CS8618, CS0169

internal class MyAspect : FieldAspect
{
    public override void BuildAspect( IAspectBuilder<IField> builder )
    {
        builder.AddContract( nameof(Filter), args: new { p = "hey" } );
    }

    [Template]
    public void Filter( dynamic? value, [CompileTime] string p )
    {
        Console.WriteLine( p );
    }
}

// <target>
internal class Target
{
    [MyAspect]
    private string? q;
}