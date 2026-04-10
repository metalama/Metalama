// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_UnrelatedConstructorIgnored;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Apply the advice to a single constructor. The overloading strategy is evaluated per mutated constructor,
        // so the unmutated constructor must remain unchanged and no forwarder is created for it.
        var target = builder.Target.Constructors.First( c => c.Parameters.Count == 1 && c.Parameters[0].Type.SpecialType == SpecialType.Int32 );

        builder.With( target )
            .IntroduceParameter(
                "creationTime",
                typeof(DateTime),
                pullStrategy: PullStrategy.UseExpression( ExpressionFactory.Parse( "global::System.DateTime.Now" ) ),
                overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
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

    // This ctor is not mutated by the advice — should be left unchanged and no forwarder created for it.
    public A( string name )
    {
        this.Name = name;
    }

    public int Id { get; }
    public string? Name { get; }
}
