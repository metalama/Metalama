// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Record_ExplicitCtor;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "OnConstructed!" );
    }
}

// <target>
[TheAspect]
public sealed record TargetRecord( int X )
{
    // Explicit ctor chaining to the primary — only the primary gets the epilogue; this ctor
    // delegates via `: this(0)` so it must be skipped by the `InitializerKind != ConstructorInitializerKind.This` filter.
    public TargetRecord() : this( 0 ) { }
}
