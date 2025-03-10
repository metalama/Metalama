// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0414

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.CallIntroductionFromCompileTimeOnlyError;

public class Aspect : TypeAspect
{
    [Introduce]
    private int f;

    [CompileTime]
    public void CompileTimeMethod()
    {
        f = 5;
    }
}

[Aspect]
internal class T { }