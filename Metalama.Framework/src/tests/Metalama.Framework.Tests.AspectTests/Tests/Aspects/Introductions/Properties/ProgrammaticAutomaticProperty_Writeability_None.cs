// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AutomaticPropertyWriteabilityNone;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Writeability.None means a read-only property with a getter implementation.
        // This is not an automatic property, so IntroduceAutomaticProperty should throw.
        builder.IntroduceAutomaticProperty( "NonWriteableProp", typeof(int), buildProperty: p => p.Writeability = Writeability.None );
    }
}

// <target>
[MyAspect]
public class C { }
