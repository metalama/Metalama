// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Target_Record_Pull;

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter( "p", typeof(int), TypedConstant.Create( 15 ), ( p, c ) => PullAction.UseExpression( TypedConstant.Create( 51 ) ) );
    }
}

// <target>
public record R
{
    [MyAspect]
    public R() { }

    public R( string s ) : this() { }
}

// <target>
public record S1 : R { }

// <target>
public record S2() : R() { }