// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.ReplaceParameter_MultiLevel_DesignTime;

public interface ICovariant<out T> { }

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
public partial class Base
{
    public Base( int x )
    {
        this.X = x;
    }

    public int X { get; }
}

// <target>
public partial class Middle : Base
{
    public Middle( int x ) : base( x ) { }
}

// <target>
public partial class Derived : Middle
{
    public Derived( int x ) : base( x ) { }
}
