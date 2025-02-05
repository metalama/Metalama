using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.CodeModel.IntroductionsFromGenericBaseClass;

[Inheritable]
public class MyInheritableAspectWhichIntroducesAMethod : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    public void M() { }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var m = builder.Target.AllMethods.OfName( "M" ).SingleOrDefault();

        if (m != null)
        {
            builder.IntroduceMethod( nameof(CallM), args: new { m } );
        }
    }

    [Template]
    private void CallM( IMethod m )
    {
        m.Invoke();
    }
}

public class AspectWhichCallsTheMethod : TypeAspect { }

// <target>
[MyInheritableAspectWhichIntroducesAMethod]
internal class Foo<T> { }

// <target>
internal class Bar : Foo<int> { }