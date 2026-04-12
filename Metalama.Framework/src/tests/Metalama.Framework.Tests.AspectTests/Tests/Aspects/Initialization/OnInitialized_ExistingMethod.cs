// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_ExistingMethod;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "From aspect!" );
    }
}

// <target>
[TheAspect]
public class TargetCode : IInitializable
{
    public virtual void Initialize( InitializationContext context )
    {
        Console.WriteLine( "Hand-authored!" );
    }
}
