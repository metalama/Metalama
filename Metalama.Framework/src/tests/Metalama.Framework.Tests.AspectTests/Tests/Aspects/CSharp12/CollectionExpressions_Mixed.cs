// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.CollectionExpressions_Mixed;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        List<string> mixed = [meta.Target.Method.Name, meta.Target.Parameters[0].Value];
        Console.WriteLine( mixed.Count );

        return meta.Proceed();
    }
}

public class C
{
    // <target>
    [TheAspect]
    public void Method( string p ) { }
}
