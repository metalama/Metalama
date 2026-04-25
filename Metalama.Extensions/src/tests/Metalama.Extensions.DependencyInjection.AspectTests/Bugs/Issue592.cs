// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// https://github.com/metalama/Metalama/issues/592

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.Issue592;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a nested class.
        var introducedType = builder.IntroduceClass( "GeneratedHelper" );

        // Introduce a dependency on the introduced type WITHOUT calling IntroduceConstructor first.
        // This should work, but currently fails unless IntroduceConstructor is called first.
        introducedType.IntroduceDependency( typeof(IFormatProvider), new DependencyOptions() { MemberName = "_formatProvider" } );
    }
}

// <target>
[MyAspect]
public class TargetClass { }