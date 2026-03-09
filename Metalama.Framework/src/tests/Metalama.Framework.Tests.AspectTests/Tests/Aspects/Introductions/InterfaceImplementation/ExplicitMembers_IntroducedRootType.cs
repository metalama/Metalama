// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedRootType;

/*
 * Tests that explicit interface members can be introduced on introduced root types (issue #601).
 */

public interface IInterface
{
    int InterfaceMethod( int i );

    int Property { get; set; }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.With( builder.Target.ContainingNamespace ).IntroduceClass( "TestType" );
        var explicitImplementation = type.ImplementInterface( typeof(IInterface) ).ExplicitMembers;

        explicitImplementation.IntroduceMethod( nameof(InterfaceMethod) );
        explicitImplementation.IntroduceProperty( nameof(Property) );
    }

    [Template]
    public int InterfaceMethod( int i )
    {
        Console.WriteLine( "This is introduced interface member." );

        return i;
    }

    [Template]
    public int Property
    {
        get
        {
            Console.WriteLine( "This is introduced interface member." );

            return 42;
        }

        set
        {
            Console.WriteLine( "This is introduced interface member." );
        }
    }
}

// <target>
[Introduction]
public class TargetClass { }
