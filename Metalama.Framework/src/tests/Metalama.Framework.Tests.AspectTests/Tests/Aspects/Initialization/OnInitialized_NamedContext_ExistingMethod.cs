// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_NamedContext_ExistingMethod;

// Verifies that when the target type already declares Initialize with a non-default parameter name
// (here: "ctx"), the template body is inlined into that method and any references in the template's
// own run-time InitializationContext parameter (here: "myCtx") are rewritten to use the hand-authored
// parameter name "ctx".

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate( InitializationContext myCtx )
    {
        Console.WriteLine( $"From aspect, intent={myCtx.Intent}" );
    }
}

// <target>
[TheAspect]
public class TargetCode : IInitializable
{
    public virtual void Initialize( InitializationContext ctx )
    {
        Console.WriteLine( $"Hand-authored, intent={ctx.Intent}" );
    }
}
