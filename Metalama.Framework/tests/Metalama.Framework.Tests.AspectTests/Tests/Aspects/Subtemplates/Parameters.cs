// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters;

internal class Aspect : TypeAspect
{
    [Introduce]
    private int Add( int a )
    {
        AddImpl( a, 1, meta.CompileTime( 1 ), 1, meta.CompileTime( 1 ) );

        throw new Exception();
    }

    [Template]
    private void AddImpl( int a, int b, int c, [CompileTime] int d, [CompileTime] int e )
    {
        meta.Return( a + b + c + d + e );
    }
}

// <target>
[Aspect]
internal class TargetCode { }