using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ImplicitProperty_AccessorMismatch
{
    /*
     * Error when accessors of implicit property interface member don't match the interface.
     */

    public interface IInterface
    {
        int TemplateWithInit { set; }

        int TemplateWithoutInit { init; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = false )]
        public int TemplateWithInit
        {
            init { }
        }

        [InterfaceMember( IsExplicit = false )]
        public int TemplateWithoutInit
        {
            set { }
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}