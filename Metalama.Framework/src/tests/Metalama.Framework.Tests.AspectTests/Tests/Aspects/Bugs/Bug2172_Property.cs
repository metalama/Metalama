// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The file intentionally has no using directive for Metalama.Framework.Aspects.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug2172_Property;

public class Aspect : Metalama.Framework.Aspects.TypeAspect
{
    [Metalama.Framework.Aspects.Introduce]
    [Metalama.Framework.Aspects.Template]
    private int P { get; set; }
}

// <target>
[Aspect]
internal class C { }
