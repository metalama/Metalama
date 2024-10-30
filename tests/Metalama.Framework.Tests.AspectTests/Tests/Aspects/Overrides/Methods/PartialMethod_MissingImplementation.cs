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