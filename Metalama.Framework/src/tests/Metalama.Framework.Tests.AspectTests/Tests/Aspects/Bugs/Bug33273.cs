// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33273;

[Inheritable]
public sealed class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        _ = meta.Cast( meta.Target.Method.ReturnType, StaticClass.StaticMethod() );

        return meta.Proceed();
    }
}

// <target>
public partial class TargetClass
{
    [TestAspect]
    public int Foo()
    {
        return 42;
    }
}

public class StaticClass
{
    public static double StaticMethod() => Math.PI;
}