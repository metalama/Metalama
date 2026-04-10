// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_DerivedClass;

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

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The overloading strategy decides per-ctor whether to generate a forwarder; it travels with the transitive
        // aspect into derived types, so the aspect itself does NOT need to enumerate derived types.
        foreach ( var constructor in builder.Target.Constructors )
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

// <target>
[MyAspect]
public class Base
{
    public Base( int id )
    {
        this.Id = id;
    }

    public int Id { get; }
}

// <target>
public class Derived : Base
{
    public Derived( int id, string name ) : base( id )
    {
        this.Name = name;
    }

    public string Name { get; }
}
