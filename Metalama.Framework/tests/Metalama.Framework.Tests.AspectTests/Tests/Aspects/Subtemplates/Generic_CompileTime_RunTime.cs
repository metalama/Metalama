// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime_RunTime;

internal class Aspect : TypeAspect
{
    [Introduce]
    private void Introduced<T>()
    {
        Console.WriteLine( $"introduced T={typeof(T)}" );

        CalledTemplate2<T>();
        CalledTemplate2<T[]>();
        CalledTemplate2<List<T>>();
    }

    [Template]
    private void CalledTemplate2<[CompileTime] T>()
    {
        Console.WriteLine( $"called template T={typeof(T)}" );
    }
}

// <target>
[Aspect]
internal class TargetCode { }