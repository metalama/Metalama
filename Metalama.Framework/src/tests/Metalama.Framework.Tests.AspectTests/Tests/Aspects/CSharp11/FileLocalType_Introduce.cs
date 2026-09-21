// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_Introduce;

// A member introduced into a file-local type has no symbol of its own, so its identifier takes the discriminator from
// the type that declares it. See issue #662. This test guards the transformation itself, which the compile-time
// pipeline performs without the identifier; the identifier is what the design-time pipeline requires.

public class IntroduceMembersAttribute : TypeAspect
{
    [Introduce]
    public int IntroducedMethod( int a )
    {
        return a;
    }

    [Introduce]
    public int IntroducedProperty { get; set; }

    [Introduce]
    public event EventHandler? IntroducedEvent;
}

// <target>
[IntroduceMembers]
file class FileLocalTarget
{
    public int Existing { get; set; }
}
