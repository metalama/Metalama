// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_MarkObsoleteDefault;

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
                    pullStrategy: PullStrategy.UseExpression( ExpressionFactory.Parse( "global::System.DateTime.Now" ) ),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors.WithObsoleteAttribute() );
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
