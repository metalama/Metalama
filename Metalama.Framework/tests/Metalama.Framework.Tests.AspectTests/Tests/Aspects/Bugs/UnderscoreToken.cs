// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.UnderscoreToken;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void TheMethod()
    {
        // This uses to fail because of two parameters named _. 
        var x = meta.RunTime( new Action<string, string>( ( _, _ ) => { } ) );
    }
}

// <target>
[TheAspect]
internal class C;