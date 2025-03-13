// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.IntroducedParameter_PrimaryRecord;

/*
 * Tests OverrideConstructor advice with IntroduceParameter on a record class with primary constructor.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single( p => !p.IsImplicitlyDeclared ) ).Override( nameof(Template), args: new { i = 1 } );

        builder.With( builder.Target.Constructors.Single( p => !p.IsImplicitlyDeclared ) )
            .IntroduceParameter(
                "introduced",
                TypeFactory.GetType( SpecialType.Int32 ),
                TypedConstant.Create( 42 ) );

        builder.With( builder.Target.Constructors.Single( p => !p.IsImplicitlyDeclared ) ).Override( nameof(Template), args: new { i = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the override {i}." );

        foreach (var param in meta.Target.Parameters)
        {
            Console.WriteLine( $"Param {param.Name} = {param.Value}" );
        }

        meta.Proceed();
    }
}

// <target>
[Override]
public record class TargetClass( int x )
{
    public int Z = x;
}