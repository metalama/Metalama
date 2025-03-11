// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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