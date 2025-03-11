// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime_CompileTime;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate<IType?>( meta.Target.Type );

        return default;
    }

    [Template]
    private void CalledTemplate<[CompileTime] T>( T x )
    {
        Console.WriteLine( $"called template T={typeof(T)} x={x}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}