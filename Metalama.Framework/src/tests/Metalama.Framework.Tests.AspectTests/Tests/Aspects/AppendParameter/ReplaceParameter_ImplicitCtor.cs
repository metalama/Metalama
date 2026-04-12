// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.ReplaceParameter_ImplicitCtor;

/// <summary>
/// A covariant interface used to test parameter type replacement with implicit constructors.
/// </summary>
public interface ICovariant<out T> { }

/// <summary>
/// Aspect that introduces an <c>ICovariant{T}</c> parameter (where T = declaring type)
/// and uses <c>IntroduceParameterAndPull</c> with <c>reuseExistingParameterOfCompatibleType</c>.
/// This test reproduces the DI LoggerDerived bug: both classes have implicit constructors.
/// </summary>
[Inheritable]
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var covariantType = builder.Target.Compilation.Types.OfName( "ICovariant" ).Single()
            .WithTypeArguments( builder.Target );

        foreach ( var constructor in builder.Target.Constructors )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "service",
                    covariantType,
                    TypedConstant.Default( covariantType ),
                    PullStrategy.IntroduceParameterAndPull(
                        "service",
                        covariantType,
                        TypedConstant.Default( covariantType ),
                        reuseExistingParameterOfCompatibleType: true ) );
        }
    }
}

// <target>
[MyAspect]
public class Base
{
}

// <target>
public class Derived : Base
{
}
