// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618, CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Tags;

internal class MyAspect : FieldAspect
{
    public override void BuildAspect( IAspectBuilder<IField> builder )
    {
        builder.AddContract( nameof(Filter), tags: new { tag = "tag" } );
    }

    [Template]
    public void Filter( dynamic? value )
    {
        Console.WriteLine( (string?)meta.Tags["tag"] );
    }
}

// <target>
internal class Target
{
    [MyAspect]
    private string? q;
}