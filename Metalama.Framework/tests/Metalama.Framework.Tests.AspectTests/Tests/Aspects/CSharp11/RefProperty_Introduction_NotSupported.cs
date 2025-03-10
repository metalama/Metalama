// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.RefProperty_Introduction_NotSupported;

public class TheAspect : TypeAspect
{
    [Introduce]
    private int _x;

    [Introduce]
    public ref int X => ref _x;
}

[TheAspect]
internal class C { }