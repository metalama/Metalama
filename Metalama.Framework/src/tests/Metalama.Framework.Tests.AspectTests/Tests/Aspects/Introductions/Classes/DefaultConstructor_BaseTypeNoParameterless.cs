// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

// https://github.com/metalama/Metalama/issues/607
// Verifies that when an introduced type's base class has no accessible parameterless constructor,
// the user must introduce a constructor that provides the constructor initializer.
// In this test, the user introduces a constructor with a base initializer, which is the correct approach.
// Also verifies that HasDefaultConstructor is false (since the only constructor has parameters).

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.DefaultConstructor_BaseTypeNoParameterless;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass(
            "GeneratedClass",
            buildType: t =>
            {
                t.BaseType = TypeFactory.GetNamedType( typeof(BaseWithNoParameterless) );
            } );

        // Introduce a constructor that chains to the base constructor.
        introducedType.IntroduceConstructor(
            nameof(ConstructorTemplate),
            buildConstructor: c =>
            {
                var p = c.AddParameter( "value", typeof(int) );
                c.InitializerKind = ConstructorInitializerKind.Base;
                c.AddInitializerArgument( p );
            } );

        // Verify that HasDefaultConstructor is false (the only constructor has parameters).
        if ( introducedType.Declaration.HasDefaultConstructor )
        {
            throw new InvalidOperationException( "HasDefaultConstructor should be false when only a parameterized constructor exists." );
        }
    }

    [Template]
    public void ConstructorTemplate() { }
}

public class BaseWithNoParameterless
{
    public BaseWithNoParameterless( int value ) { }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
