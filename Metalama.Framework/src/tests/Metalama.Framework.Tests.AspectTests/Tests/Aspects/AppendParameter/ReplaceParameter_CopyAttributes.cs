// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.ReplaceParameter_CopyAttributes;

/// <summary>
/// A covariant interface used to test parameter type replacement.
/// </summary>
public interface ICovariant<out T> { }

/// <summary>
/// A custom attribute applied to the introduced parameter.
/// After replacement, the attribute should still be present.
/// </summary>
[AttributeUsage( AttributeTargets.Parameter )]
public class MyCustomAttribute : Attribute { }

/// <summary>
/// Aspect that introduces an <c>ICovariant{T}</c> parameter with a custom attribute,
/// using <c>reuseExistingParameterOfCompatibleType</c>.
/// Derived constructors should replace the type but keep all custom attributes.
/// </summary>
[Inheritable]
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var covariantType = ((INamedType) builder.Target.Compilation.Types.OfName( "ICovariant" ).Single())
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
                        reuseExistingParameterOfCompatibleType: true ),
                    ImmutableArray.Create( AttributeConstruction.Create( typeof(MyCustomAttribute) ) ) );
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
