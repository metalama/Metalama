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
        // A compile-time element followed by a run-time element.
        List<string> first = [meta.Target.Method.Name, meta.Target.Parameters[0].Value];
        Console.WriteLine( first.Count );

        // A run-time element followed by a compile-time element.
        List<string> last = [meta.Target.Parameters[0].Value, meta.Target.Method.Name];
        Console.WriteLine( last.Count );

        // A compile-time element between two run-time elements.
        int[] middle = [meta.Target.Parameters[1].Value, meta.Target.Parameters.Count, meta.Target.Parameters[1].Value];
        Console.WriteLine( middle.Length );

        // A compile-time spread element followed by a run-time element.
        List<string> spread = [..new[] { meta.Target.Method.Name, meta.Target.Type.Name }, meta.Target.Parameters[0].Value];
        Console.WriteLine( spread.Count );

        return meta.Proceed();
    }
}

public class C
{
    // <target>
    [TheAspect]
    public void Method( string p, int i ) { }
}
