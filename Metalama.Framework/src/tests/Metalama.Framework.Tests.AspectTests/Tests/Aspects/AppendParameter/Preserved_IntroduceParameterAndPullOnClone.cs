// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_IntroduceParameterAndPullOnClone;

// When a custom pull strategy returns IntroduceParameterAndPull without supplying a ForwarderExpression,
// the framework falls back to default(T) (with the null-forgiving operator for non-nullable reference types)
// so the emitted forwarder always compiles.
public sealed class FallbackPullStrategy : IPullStrategy
{
    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        return PullAction.IntroduceParameterAndPull( pulledParameter.Name, pulledParameter.Type, parameterDefaultValue: null );
    }
}

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "creationTime",
                    typeof(DateTime),
                    pullStrategy: new FallbackPullStrategy(),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
        }
    }
}

// <target>
[MyAspect]
public class A
{
    public A( int id )
    {
        this.Id = id;
    }

    public int Id { get; }
}
