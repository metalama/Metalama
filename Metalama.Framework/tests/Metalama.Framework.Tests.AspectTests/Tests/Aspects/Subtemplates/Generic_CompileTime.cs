// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate<int>( 1, 2, 3 );

        return default;
    }

    [Template]
    private void CalledTemplate<[CompileTime] T>( int i, [CompileTime] int j, T k )
    {
        Console.WriteLine( $"called template T={typeof(T)} i={i} j={j} k={k}" );

        CalledTemplate2<T>();

        CalledTemplate2<T[]>();

        CalledTemplate2<Dictionary<int, T>>();

        CalledTemplate2<TargetCode>();
    }

    [Template]
    private void CalledTemplate2<[CompileTime] T>()
    {
        Console.WriteLine( $"called template 2 T={typeof(T)}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}