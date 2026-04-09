// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Spec §1.6: Range validation with hand-authored IInitializable and required init properties.

#if TEST_OPTIONS
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if NET7_0_OR_GREATER

using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_RangeValidation;

public class Range : IInitializable
{
    public required int Min { get; init; }

    public required int Max { get; init; }

    public Range( InitializationContext context = default ) { }

    public virtual void Initialize( InitializationContext context )
    {
        if ( Max < Min )
        {
            throw new InvalidOperationException( "Max must not be less than Min." );
        }
    }
}

public class NamedRange : Range
{
    public required string Name { get; init; }

    public NamedRange( InitializationContext context = default ) : base( context ) { }

    public override void Initialize( InitializationContext context )
    {
        base.Initialize( context.Descend() );

        if ( string.IsNullOrWhiteSpace( Name ) )
        {
            throw new InvalidOperationException( "Name must not be empty." );
        }
    }
}

// <target>
public class Caller
{
    public void Method()
    {
        // Object initializer — Linker wraps with Initialized()
        var r = new Range { Min = 1, Max = 12 };

        // Derived type — no cast needed with generic Initialized<T>
        var nr = new NamedRange { Name = "test", Min = 1, Max = 10 };

    }
}

#endif
