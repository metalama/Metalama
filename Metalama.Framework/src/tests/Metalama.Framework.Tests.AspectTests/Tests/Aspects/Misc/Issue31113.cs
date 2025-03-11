// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31113;

public class MyAspect : TypeAspect
{
    [Introduce]
    public void Method()
    {
        var method = meta.Target.Type.Methods.Single();
        method.Invoke();
    }
}

[MyAspect]
internal class C
{
    private void M() { }
}