// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.ReplaceParameter_SourceDefined;

/// <summary>
/// A covariant interface.
/// </summary>
public interface ICovariant<out T> { }

/// <summary>
/// Inheritable aspect that introduces an <c>ICovariant{T}</c> parameter with
/// <c>reuseExistingParameterOfCompatibleType</c>. When the derived class has a hand-written
/// parameter of the same name but incompatible type, the source-defined parameter is
/// left untouched and a new parameter with a deduplicated name is introduced instead.
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
    public Base( int x )
    {
        this.X = x;
    }

    public int X { get; }
}

// <target>
public class Derived : Base
{
    public Derived( int x, ICovariant<object> service ) : base( x )
    {
    }
}
