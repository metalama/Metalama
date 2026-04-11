// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_InheritedBase;

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

// Hand-authored base exposes a virtual OnConstructed and a constructor accepting
// InitializationContext that calls it, guarded by IsHandled so derived layers can skip.
// The derived TargetCode overrides OnConstructed and its `:base(context.Descend(OnConstructed))`
// call makes the base constructor's guard fire.
public class BaseClass
{
    public BaseClass( InitializationContext context = default )
    {
        if ( !context.IsHandled( InitializationSlot.OnConstructed ) )
        {
            this.OnConstructed( context );
        }
    }

    public virtual void OnConstructed( InitializationContext context = default )
    {
    }
}

// <target>
[TheAspect]
public class TargetCode : BaseClass
{
    public TargetCode()
    {
    }
}
