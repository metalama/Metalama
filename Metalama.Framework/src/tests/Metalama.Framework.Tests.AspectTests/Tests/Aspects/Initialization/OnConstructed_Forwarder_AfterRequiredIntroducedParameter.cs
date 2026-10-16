// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_AfterRequiredIntroducedParameter;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(RequiredParameterAspect), typeof(InitializerAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_AfterRequiredIntroducedParameter;

/// <summary>
/// Introduces a required parameter without a forwarding constructor.
/// </summary>
public class RequiredParameterAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This ) )
        {
            builder.With( constructor ).IntroduceParameter( "p", typeof(int), overloadingStrategy: new NoForwarderStrategy() );
        }
    }
}

[CompileTime]
public sealed class NoForwarderStrategy : IConstructorOverloadingStrategy
{
    public ConstructorOverloadingAction GetConstructorOverloadingAction( IConstructor mutatedConstructor, IParameter introducedParameter )
        => ConstructorOverloadingAction.None;
}

public class InitializerAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed {meta.Target.Type.Name}" );
    }
}

// <target>
[RequiredParameterAspect]
[InitializerAspect]
public class TargetCode
{
    public TargetCode() { }
}
