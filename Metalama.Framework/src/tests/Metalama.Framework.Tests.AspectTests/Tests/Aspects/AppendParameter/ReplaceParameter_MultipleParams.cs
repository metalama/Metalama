// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.ReplaceParameter_MultipleParams;

/// <summary>
/// A covariant interface used to test parameter type replacement.
/// </summary>
public interface ICovariant<out T> { }

/// <summary>
/// A non-covariant service interface — should NOT be replaced, only reused as-is.
/// </summary>
public interface IService { }

/// <summary>
/// Aspect that introduces both <c>ICovariant{T}</c> (covariant, replaced on derived types)
/// and <c>IService</c> (non-covariant, reused as-is on derived types).
/// </summary>
[Inheritable]
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var covariantType = ((INamedType) builder.Target.Compilation.Types.OfName( "ICovariant" ).Single())
            .WithTypeArguments( builder.Target );

        var serviceType = (INamedType) builder.Target.Compilation.Types.OfName( "IService" ).Single();

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

            builder.With( constructor )
                .IntroduceParameter(
                    "svc",
                    serviceType,
                    TypedConstant.Default( serviceType ),
                    PullStrategy.IntroduceParameterAndPull(
                        "svc",
                        serviceType,
                        TypedConstant.Default( serviceType ),
                        reuseExistingParameterOfCompatibleType: true ) );
        }
    }
}

// <target>
[MyAspect]
public class Base
{
    public Base( int x )
    {
        this.X = x;
    }

    public int X { get; }
}

// <target>
public class Derived : Base
{
    public Derived( int x, string name ) : base( x )
    {
        this.Name = name;
    }

    public string Name { get; }
}
