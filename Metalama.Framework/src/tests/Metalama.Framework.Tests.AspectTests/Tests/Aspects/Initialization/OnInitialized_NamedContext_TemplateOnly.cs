// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_NamedContext_TemplateOnly;

// Verifies that when the template's run-time InitializationContext parameter has a non-default name
// (here: "myCtx") and the target type has no hand-authored Initialize method, the introduced
// Initialize method uses the default parameter name "context", and template references to "myCtx"
// are rewritten to "context" in the inlined body.

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate( InitializationContext myCtx )
    {
        Console.WriteLine( $"Initialized, intent={myCtx.Intent}" );
    }
}

// <target>
[TheAspect]
public class TargetCode
{
}
