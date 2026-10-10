// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug2155;

/// <summary>
/// Calls a member on a local whose type has a <c>dynamic</c> generic argument. The template compiler must report LAMA0227
/// and must not fail with an internal error on the member access.
/// </summary>
public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var values = new List<dynamic?>();

        foreach ( var parameter in meta.Target.Parameters )
        {
            values.Add( parameter.Value );
            values?.Add( parameter.Value );
        }

        var count = values.Count;

        return meta.Proceed();
    }
}

// <target>
public class C
{
    [TheAspect]
    public void M( int a, string b ) { }
}
