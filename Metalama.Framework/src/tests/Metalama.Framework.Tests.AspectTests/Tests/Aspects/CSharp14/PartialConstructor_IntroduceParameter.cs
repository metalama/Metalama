// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialConstructor_IntroduceParameter;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).IntroduceParameter( "p", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[TheAspect]
internal partial class ClassWithPartialConstructor
{
    public partial ClassWithPartialConstructor( int x );

    public partial ClassWithPartialConstructor( int x )
    {
        Console.WriteLine( $"x={x}" );
    }
}

// <target>
[TheAspect]
internal partial class ClassWithTwoPartialConstructors
{
    public partial ClassWithTwoPartialConstructors();

    public partial ClassWithTwoPartialConstructors()
    {
        Console.WriteLine( "Default" );
    }

    public partial ClassWithTwoPartialConstructors( string s );

    public partial ClassWithTwoPartialConstructors( string s )
    {
        Console.WriteLine( s );
    }
}

#endif
