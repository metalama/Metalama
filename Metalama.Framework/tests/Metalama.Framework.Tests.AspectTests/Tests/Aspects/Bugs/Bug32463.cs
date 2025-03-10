// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32463;

#pragma warning disable CS0169

public class BeforeCtorAttribute : TypeAspect
{
    [Introduce]
    private int f;

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.AddInitializer( nameof(BeforeInstanceConstructor), InitializerKind.BeforeInstanceConstructor );
    }

    [Template]
    private void BeforeInstanceConstructor()
    {
        Console.WriteLine( "before ctor" );
    }
}

// <target>
[BeforeCtor]
internal struct S { }