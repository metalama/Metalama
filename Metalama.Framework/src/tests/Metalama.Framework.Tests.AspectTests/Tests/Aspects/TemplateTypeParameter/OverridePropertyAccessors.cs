// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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