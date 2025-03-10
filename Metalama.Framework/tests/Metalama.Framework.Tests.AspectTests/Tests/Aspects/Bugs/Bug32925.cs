// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32925;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var result = meta.Proceed();

        // We invoke an extension method, but as a plain method.
        ExtensionClass.ExtensionMethod( result );

        return result;
    }
}

public static class ExtensionClass
{
    public static void ExtensionMethod( this object o ) { }
}

// <target>
public class C
{
    [TheAspect]
    public void M() { }
}