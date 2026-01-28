// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.WithParameterAttributeModification;

/*
 * Regression test: When an aspect both modifies parameter attributes AND introduces a parameter,
 * the IntroduceParameter transformation should still be applied. This tests that the transformation
 * lookup uses the original syntax node, not the rewritten node after visiting children.
 */

public class MyAttribute : Attribute { }

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors )
        {
            // First, add an attribute to the existing parameter.
            // This will cause the parameter node to change during VisitParameter.
            foreach ( var parameter in constructor.Parameters )
            {
                builder.With( parameter ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
            }

            // Then, introduce a new parameter.
            // If the bug exists, this transformation will be lost because the constructor node changed.
            builder.With( constructor ).IntroduceParameter( "introduced", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[MyAspect]
internal class C
{
    public C( int x )
    {
        Console.WriteLine( $"x={x}" );
    }
}
