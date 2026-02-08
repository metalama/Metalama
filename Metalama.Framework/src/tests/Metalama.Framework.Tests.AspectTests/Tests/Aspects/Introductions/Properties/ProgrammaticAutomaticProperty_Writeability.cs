// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AutomaticPropertyWriteability;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Get-only auto property (cannot be set at all).
        builder.IntroduceAutomaticProperty( "NonWriteableProp", typeof(int), buildProperty: p => p.Writeability = Writeability.None );

        // Constructor-only auto property (can be set in constructor only).
        builder.IntroduceAutomaticProperty( "ConstructorOnlyProp", typeof(int), buildProperty: p => p.Writeability = Writeability.ConstructorOnly );

        // Init-only auto property (can be set in constructor and object initializers).
        builder.IntroduceAutomaticProperty( "InitOnlyProp", typeof(int), buildProperty: p => p.Writeability = Writeability.InitOnly );

        // Regular auto property with setter.
        builder.IntroduceAutomaticProperty( "ReadWriteProp", typeof(int), buildProperty: p => p.Writeability = Writeability.All );
    }
}

// <target>
[MyAspect]
public class C { }
