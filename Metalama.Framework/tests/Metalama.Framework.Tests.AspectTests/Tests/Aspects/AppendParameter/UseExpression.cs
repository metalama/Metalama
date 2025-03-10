// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.UseExpression;

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter(
            "p",
            typeof(DateTime),
            TypedConstant.Default( typeof(DateTime) ),
            ( parameter, constructor ) => PullAction.UseExpression( ExpressionFactory.Parse( "System.DateTime.Now" ) ) );
    }
}

// <target>
public class C
{
    [MyAspect]
    public C() { }

    public C( string s ) : this() { }
}

// <target>
public class D : C { }