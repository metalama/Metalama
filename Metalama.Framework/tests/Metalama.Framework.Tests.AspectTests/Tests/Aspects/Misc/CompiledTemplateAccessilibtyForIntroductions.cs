// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibtyForIntroductions;

internal class MyAspect : TypeAspect
{
    [Introduce]
    private void Private() { }

    [Introduce]
    protected void Protected() { }

    [Introduce]
    internal void Internal() { }

    [Introduce]
    public void Public() { }

    [Introduce]
    private protected void PrivateProtected() { }

    [Introduce]
    protected internal void ProtectedInternal() { }
}

// <target>
[MyAspect]
internal class Target { }