// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_TwoAspects_PartialForwarding;

// The target type has two source constructors: the parameterless one and Order(int id). The first
// aspect (Timestamped) uses ForwardDefaultConstructor, so only the parameterless constructor receives
// a forwarder. The second aspect (Tracked) uses ForwardSourceConstructors. The resulting pipeline
// exercises the scenario where, after the first aspect mutates both constructors, the unmutated
// source constructor Order(int id) is no longer present (it has been mutated too, just without a
// forwarder). When the second aspect pulls into the existing forwarder emitted by the first aspect,
// the framework must use the forwarder expression supplied by the pull strategy for that forwarder.
// When the second aspect mutates Order(int id, DateTime _) and emits its own forwarder Order(int id),
// that new forwarder must likewise receive the strategy-supplied forwarder expression.

public class TimestampedAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors.Where( c => c.Origin.Kind == DeclarationOriginKind.Source ) )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "creationTime",
                    typeof(DateTime),
                    pullStrategy: PullStrategy.IntroduceParameterAndPull(
                        forwarderExpression: ExpressionFactory.Parse( "global::System.DateTime.Now" ) ),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardDefaultConstructor );
        }
    }
}

public class TrackedAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors.Where( c => c.Origin.Kind == DeclarationOriginKind.Source ) )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "traceId",
                    typeof(Guid),
                    pullStrategy: PullStrategy.IntroduceParameterAndPull(
                        forwarderExpression: ExpressionFactory.Parse( "global::System.Guid.NewGuid()" ) ),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
        }
    }
}

// <target>
[Timestamped]
[Tracked]
public class Order
{
    public Order() { }

    public Order( int id )
    {
        this.Id = id;
    }

    public int Id { get; }
}
