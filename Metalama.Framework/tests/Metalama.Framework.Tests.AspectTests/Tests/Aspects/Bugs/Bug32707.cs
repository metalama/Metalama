#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32707;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(MyOverride1Aspect), typeof(MyContractAspect), typeof(MyOverride2Aspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32707;

public enum TestEnum
{
    Zero,
    Default
}

public class MyOverride1Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Override1" );
        _ = meta.Proceed();

        return meta.Proceed();
    }
}

public class MyOverride2Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Override2" );
        _ = meta.Proceed();

        return meta.Proceed();
    }
}

public class MyContractAspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        Console.WriteLine( $"Contract on {meta.Target.Parameter.Name}" );
    }
}

// <target>
internal class TargetClass
{
    // Overrides should not have default values, only the original declaration.
    [MyOverride1Aspect]
    [MyOverride2Aspect]
    public int Method(
        [MyContractAspect] int intParam = 42,
        [MyContractAspect] object? objectParam = null,
        [MyContractAspect] TestEnum enumParam = TestEnum.Default )
    {
        return 42;
    }

    // Overrides should not have default values, only the original declaration.
    [MyOverride1Aspect]
    [MyOverride2Aspect]
    public async Task<int> AsyncMethod(
        [MyContractAspect] int intParam = 42,
        [MyContractAspect] object? objectParam = null,
        [MyContractAspect] TestEnum enumParam = TestEnum.Default )
    {
        await Task.Yield();

        return 42;
    }

    // Overrides should not have default values, only the original declaration.
    [MyOverride1Aspect]
    [MyOverride2Aspect]
    public IEnumerable<int> IteratorMethod(
        [MyContractAspect] int intParam = 42,
        [MyContractAspect] object? objectParam = null,
        [MyContractAspect] TestEnum enumParam = TestEnum.Default )
    {
        yield return 42;
    }

    // Overrides should not have default values, only the original declaration.
    [MyOverride1Aspect]
    [MyOverride2Aspect]
    public async IAsyncEnumerable<int> AsyncIteratorMethod(
        [MyContractAspect] int intParam = 42,
        [MyContractAspect] object? objectParam = null,
        [MyContractAspect] TestEnum enumParam = TestEnum.Default )
    {
        await Task.Yield();

        yield return 42;
    }
}