// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

// https://github.com/metalama/Metalama/issues/607
// Verifies that when HasDefaultConstructor is set to false on the builder,
// no implicit constructor is added to the code model.
// The user is responsible for correctness and introducing at least one constructor.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.DefaultConstructor_SetFalse;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass(
            "GeneratedClass",
            buildType: t =>
            {
                t.HasDefaultConstructor = false;
            } );

        // Verify HasDefaultConstructor is false.
        if ( introducedType.Declaration.HasDefaultConstructor )
        {
            throw new InvalidOperationException( "HasDefaultConstructor should be false after being set to false." );
        }

        // Verify the Constructors collection is empty.
        if ( introducedType.Declaration.Constructors.Count != 0 )
        {
            throw new InvalidOperationException(
                $"Expected 0 constructors, but found {introducedType.Declaration.Constructors.Count}." );
        }

        // Introduce an explicit constructor since the user is responsible.
        introducedType.IntroduceConstructor(
            nameof(ConstructorTemplate),
            buildConstructor: c =>
            {
                c.AddParameter( "value", typeof(int) );
            } );
    }

    [Template]
    public void ConstructorTemplate() { }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
