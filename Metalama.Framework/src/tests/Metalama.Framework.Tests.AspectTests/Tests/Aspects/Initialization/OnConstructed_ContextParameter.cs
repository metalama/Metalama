// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_ContextParameter;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    // The template declares a run-time InitializationContext parameter. The framework maps it
    // by name to the introduced OnConstructed method's own context parameter.
    [Template]
    private void InitializerTemplate( InitializationContext context )
    {
        Console.WriteLine( $"OnConstructed, intent={context.Intent}" );
    }
}

// <target>
[TheAspect]
public class TargetCode
{
    public TargetCode( int value )
    {
        _ = value;
    }
}
