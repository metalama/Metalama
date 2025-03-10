// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33671;

public class TestAspect : TypeAspect, IFace
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

    public string? ProfileName { get; init; }
}

// <target>
[TestAspect]
internal class Target { }