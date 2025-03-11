// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.ComponentModel;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Overrides.Methods.PartialMethod_MissingImplementation;

public class Override1Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"This is the override of {meta.Target.Method}." );

        return meta.Proceed();
    }
}

public class Override2Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine($"This is the override of {meta.Target.Method}.");

        var result = meta.Proceed();

        return result;
    }
}

public class Override3Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine($"This is the override of {meta.Target.Method}.");

        return default;
    }
}

// <target>
internal partial class TargetClass
{
#if TESTRUNNER
    [Override1]
    public partial int TargetMethod1();

    [Override2]
    public partial int TargetMethod2();

    [Override3]
    public partial int TargetMethod3();

    [Override1]
    public partial void TargetMethod4();

    [Override2]
    public partial void TargetMethod5();

    [Override3]
    public partial void TargetMethod6();
#endif
}