using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "normal template" );

        switch (meta.Target.Parameters["x"].Value)
        {
            case 0:
                CalledTemplate();

                break;

            case 1:
                CalledTemplate();

                break;

            case 2:
            {
                var aspect = this;
                aspect.CalledTemplate();
            }

                break;
        }

        throw new Exception();
    }

    [Template]
    protected virtual void CalledTemplate()
    {
        Console.WriteLine( "base called template" );
    }
}

internal class DerivedAspect : Aspect
{
    protected override void CalledTemplate()
    {
        Console.WriteLine( "derived called template" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method1( int x ) { }

    [DerivedAspect]
    private void Method2( int x ) { }
}