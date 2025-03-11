// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.RepeatedAnonymousTypeInTemplate;

public class AspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Enumerable.Range( 0, 10 ).Select( i => new { i } ).Select( x => x.i );
        Enumerable.Range( 0, 10 ).Select( i => new { i } ).Select( x => x.i );

        return null;
    }
}

internal class Target
{
    // <target>
    [Aspect]
    private void M() { }
}