// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Preserved_ImplicitConstructor;

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
                    pullStrategy: PullStrategy.IntroduceParameterAndPull( defaultValue: TypedConstant.Default( typeof(DateTime) ) ),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardDefaultConstructor );
        }
    }
}

// <target>
[MyAspect]
public class A
{
}

internal class Program
{
    private static T Create<T>()
        where T : new()
        => new T();

    public static void TestMain()
    {
        _ = Create<A>();
        Console.WriteLine( $"Constructors={typeof(A).GetConstructors().Length}" );
    }
}
