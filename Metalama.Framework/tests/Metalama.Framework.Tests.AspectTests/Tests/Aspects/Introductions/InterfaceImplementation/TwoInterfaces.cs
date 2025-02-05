using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TwoInterfaces
{
    /*
     * Tests introducing two interfaces to the same target.
     */

    public interface IInterface1 { }

    public interface IInterface2 { }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface1) );
            aspectBuilder.ImplementInterface( typeof(IInterface2) );
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}