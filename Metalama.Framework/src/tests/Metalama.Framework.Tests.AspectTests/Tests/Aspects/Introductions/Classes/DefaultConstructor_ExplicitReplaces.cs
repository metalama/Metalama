// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

// https://github.com/metalama/Metalama/issues/607
// Verifies that when an explicit parameterized constructor is introduced on an introduced type,
// the implicit default constructor is removed (matching C# semantics),
// and HasDefaultConstructor becomes false.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.DefaultConstructor_ExplicitReplaces;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass( "GeneratedClass" );

        // Introduce an explicit constructor with parameters.
        introducedType.IntroduceConstructor(
            nameof(ConstructorTemplate),
            buildConstructor: c =>
            {
                c.AddParameter( "value", typeof(int) );
            } );

        // Verify that HasDefaultConstructor is now false (no parameterless constructor).
        if ( introducedType.Declaration.HasDefaultConstructor )
        {
            throw new InvalidOperationException( "HasDefaultConstructor should be false after introducing a parameterized constructor." );
        }

        // Verify that there is no parameterless constructor in the collection.
        if ( introducedType.Declaration.Constructors.Count != 1 )
        {
            throw new InvalidOperationException(
                $"Expected 1 constructor, but found {introducedType.Declaration.Constructors.Count}." );
        }
    }

    [Template]
    public void ConstructorTemplate() { }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
