// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_ChainedConstructor;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Only introduce on "root" constructors (those that do not chain via :this).
        var rootCtors = builder.Target.Constructors
            .Where( c => c.InitializerKind != ConstructorInitializerKind.This )
            .ToArray();

        foreach ( var constructor in rootCtors )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "creationTime",
                    typeof(DateTime),
                    pullStrategy: PullStrategy.IntroduceParameterAndPull(
                        forwarderExpression: ExpressionFactory.Parse( "global::System.DateTime.Now" ) ),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
        }
    }
}

// <target>
[MyAspect]
public class Order
{
    public Order( int id )
    {
        this.Id = id;
    }

    public Order( int id, string label ) : this( id )
    {
        this.Label = label;
    }

    public int Id { get; }
    public string? Label { get; }
}
