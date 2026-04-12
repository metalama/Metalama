// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_NamedContext_Inheritance;

// Verifies that when a base class declares Initialize with a non-default parameter name (here: "ctx"),
// the override introduced by the aspect on the derived type uses the default parameter name "context"
// (NOT the base's parameter name — overrides may freely rename their parameters in C#), and the
// synthesized base.Initialize(...) call references the introduced parameter, not the base's.

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

public class BaseClass : IInitializable
{
    public virtual void Initialize( InitializationContext ctx )
    {
        Console.WriteLine( "Base!" );
    }
}

// <target>
[TheAspect]
public class TargetCode : BaseClass
{
}
