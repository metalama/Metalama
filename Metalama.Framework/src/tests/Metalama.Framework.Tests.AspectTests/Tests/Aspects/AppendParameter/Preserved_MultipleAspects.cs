// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_MultipleAspects;

public sealed class TimestampPullStrategy : IPullStrategy
{
    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        if ( targetMember is IConstructor ctor && ctor.IsSourceCompatibilityConstructor() )
        {
            return PullAction.UseExpression( ExpressionFactory.Parse( "global::System.DateTime.Now" ) );
        }

        return PullAction.IntroduceParameterAndPull( pulledParameter.Name, pulledParameter.Type, parameterDefaultValue: null );
    }
}

public sealed class TraceIdPullStrategy : IPullStrategy
{
    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        if ( targetMember is IConstructor ctor && ctor.IsSourceCompatibilityConstructor() )
        {
            return PullAction.UseExpression( ExpressionFactory.Parse( "global::System.Guid.NewGuid()" ) );
        }

        return PullAction.IntroduceParameterAndPull( pulledParameter.Name, pulledParameter.Type, parameterDefaultValue: null );
    }
}

public class TimestampedAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Target source constructors (regardless of whether they have been mutated by a prior aspect).
        foreach ( var constructor in builder.Target.Constructors.Where( c => c.Origin.Kind == DeclarationOriginKind.Source ) )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "creationTime",
                    typeof(DateTime),
                    pullStrategy: new TimestampPullStrategy(),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
        }
    }
}

public class TrackedAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Target source constructors (regardless of whether they have been mutated by a prior aspect).
        foreach ( var constructor in builder.Target.Constructors.Where( c => c.Origin.Kind == DeclarationOriginKind.Source ) )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "traceId",
                    typeof(Guid),
                    pullStrategy: new TraceIdPullStrategy(),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
        }
    }
}

// <target>
[Timestamped]
[Tracked]
public class Order
{
    public Order( int id )
    {
        this.Id = id;
    }

    public int Id { get; }
}
