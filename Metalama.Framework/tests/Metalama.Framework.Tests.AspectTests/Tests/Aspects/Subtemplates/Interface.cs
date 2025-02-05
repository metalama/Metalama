using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Interface;

[CompileTime]
internal interface IMyAspect : IAspect
{
    [Template]
    void CalledTemplate();
}

internal class Aspect : OverrideMethodAspect, IMyAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate();

        IMyAspect myAspect = this;
        myAspect.CalledTemplate();

        ( (IMyAspect)this ).CalledTemplate();

        return default;
    }

    [Template]
    public void CalledTemplate()
    {
        Console.WriteLine( "called template" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}