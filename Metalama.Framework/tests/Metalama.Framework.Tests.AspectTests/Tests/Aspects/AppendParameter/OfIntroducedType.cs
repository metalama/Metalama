// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.OfIntroducedType;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass( "X", buildType: b => { b.Accessibility = Accessibility.Public; } )
            .Declaration;

        builder.With( builder.Target.Constructors.Single() ).IntroduceParameter( "p", introducedType, TypedConstant.Default( introducedType ) );
    }
}

// <target>
[MyAspect]
public class C
{
    public C() { }
}