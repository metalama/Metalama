// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_NamedContext_BeforeCtor;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.BeforeInstanceConstructor );
    }

    // The template declares a run-time InitializationContext parameter using its own name ('context').
    // For BeforeInstanceConstructor templates the framework maps this parameter per-constructor to
    // the target constructor's actual InitializationContext parameter (here, 'ctx'), so the expanded
    // code should reference 'ctx.Intent' (not 'context.Intent').
    [Template]
    private void InitializerTemplate( InitializationContext context )
    {
        Console.WriteLine( $"Before ctor, intent={context.Intent}" );
    }
}

// <target>
[TheAspect]
public class TargetCode : IInitializable
{
    public int Value { get; set; }

    // Non-default parameter name 'ctx' — the linker must use this name when appending the named argument at call sites.
    public TargetCode( int value, InitializationContext ctx = default )
    {
        this.Value = value;
    }

    public virtual void Initialize( InitializationContext context = default ) { }
}

// <target>
public class Caller
{
    public void Method()
    {
        var t = new TargetCode( 42 );
    }
}
