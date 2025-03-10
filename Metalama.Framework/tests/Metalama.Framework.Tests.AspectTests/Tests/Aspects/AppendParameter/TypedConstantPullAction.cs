// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.TypedConstantPullAction;

public class AddParameter : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceParameter(
            "arg",
            typeof(int),
            TypedConstant.Default( typeof(int) ),
            ( param, ctor ) => PullAction.UseExpression( TypedConstant.Create( 42 ) ) );
    }
}

// <target>
internal class TargetCode
{
    [AddParameter]
    private TargetCode( string s ) { }

    private TargetCode( int i ) : this( i.ToString() ) { }
}