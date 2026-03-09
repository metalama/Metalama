// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

// https://github.com/metalama/Metalama/issues/607
// Verifies that an introduced type with no explicit constructors has an implicit default constructor,
// and that HasDefaultConstructor is true.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.DefaultConstructor_Implicit;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass( "GeneratedClass" );

        // Verify HasDefaultConstructor is true.
        if ( !introducedType.Declaration.HasDefaultConstructor )
        {
            throw new InvalidOperationException( "HasDefaultConstructor should be true for introduced type with no explicit constructors." );
        }

        // Verify the Constructors collection contains exactly one constructor (the implicit default).
        if ( introducedType.Declaration.Constructors.Count != 1 )
        {
            throw new InvalidOperationException(
                $"Expected 1 constructor, but found {introducedType.Declaration.Constructors.Count}." );
        }
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
