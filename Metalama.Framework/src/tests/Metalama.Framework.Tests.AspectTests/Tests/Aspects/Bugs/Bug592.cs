// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// https://github.com/metalama/Metalama/issues/592
// Introduced types should have an implicit default constructor visible in their Constructors collection,
// similar to how source types expose the implicit constructor from Roslyn.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug592;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a nested class.
        var introducedType = builder.IntroduceClass( "GeneratedHelper" );

        // Iterate over constructors and add a parameter to each.
        // For a non-static class with no explicit constructors, this should find the implicit default constructor.
        foreach ( var constructor in introducedType.Declaration.Constructors )
        {
            builder.With( constructor ).IntroduceParameter( "p", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[MyAspect]
public class TargetClass
{
}
