// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Declarative_IntroducedType;

/*
 * Tests that declarative explicit interface members ([InterfaceMember(IsExplicit = true)]) work on introduced types (issue #601).
 */

public interface IInterface
{
    int InterfaceMethod();

    int Property { get; set; }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.IntroduceClass( "TestType" );
        type.ImplementInterface( typeof(IInterface) );
    }

    [InterfaceMember( IsExplicit = true )]
    public int InterfaceMethod()
    {
        Console.WriteLine( "This is introduced interface member." );

        return meta.Proceed();
    }

    [InterfaceMember( IsExplicit = true )]
    public int Property
    {
        get
        {
            Console.WriteLine( "This is introduced interface member." );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "This is introduced interface member." );
            meta.Proceed();
        }
    }
}

// <target>
[Introduction]
public class TargetClass { }
