// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.SimpleLambdaModifier_Template;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var d1 = new TheRunTimeDelegate( (ref dt) => dt = DateTime.MaxValue );
        var d2 = new TheCompileTimeDelegate( (ref m) => m = meta.Target.Method );

        return meta.Proceed();
    }
}

public delegate void TheRunTimeDelegate( ref DateTime dt );

[CompileTime]
public delegate void TheCompileTimeDelegate( ref IMethod m );

// <target>
public class C
{
    [TheAspect]
    public void M() { }
}

#endif