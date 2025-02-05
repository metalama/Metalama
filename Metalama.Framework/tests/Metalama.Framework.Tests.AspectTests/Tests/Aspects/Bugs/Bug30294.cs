using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30294;

internal class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        try
        {
            // This could not be in one line because of the bug.
            return meta.Proceed();
        }
        catch (Exception) when (meta.Target.Parameters[0].Value)
        {
            return default;
        }
    }
}

// <target>
internal class TestClass
{
    [TestAspect]
    private async void Execute( bool param )
    {
        await Task.CompletedTask;
    }
}