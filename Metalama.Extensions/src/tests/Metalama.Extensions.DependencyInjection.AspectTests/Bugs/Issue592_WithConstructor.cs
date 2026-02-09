// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// https://github.com/metalama/Metalama/issues/592
// This test verifies the workaround: calling IntroduceConstructor before IntroduceDependency.

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.Issue592_WithConstructor;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a nested class.
        var introducedType = builder.IntroduceClass( "GeneratedHelper" );

        // Introduce a constructor first (workaround).
        introducedType.IntroduceConstructor( nameof(ConstructorTemplate) );

        // Now introduce the dependency - this should work because the constructor already exists.
        introducedType.IntroduceDependency( typeof(IFormatProvider), new DependencyOptions() { MemberName = "_formatProvider" } );
    }

    [Template]
    public void ConstructorTemplate()
    {
    }
}

// <target>
[MyAspect]
public class TargetClass
{
}
