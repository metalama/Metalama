using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33396;

public class TestAspect : MethodAspect
{
    private int _i;

    public TestAspect( int i )
    {
        _i = i;
    }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        switch (_i)
        {
            case 1:
                builder.Override( nameof(Template) );

                break;

            case 2:
                builder.Override( nameof(Template), args: new { T = builder.Target.DeclaringType.DeclaringType } );

                break;

            case 3:
                builder.Override( nameof(Template), args: new { T = "X" } );

                break;
        }
    }

    [Template]
    public void Template<[CompileTime] T>() { }
}

// <target>
internal class Target
{
    [TestAspect( 1 )]
    public void M1() { }

    [TestAspect( 2 )]
    public void M2() { }

    [TestAspect( 3 )]
    public void M3() { }
}