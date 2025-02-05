#if TEST_OPTIONS
// @Skipped(Case for interface merge conflict resolution, not implemented.)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExistingImplicitInterfaceImplementation
{
    /*
     * When the target class already explicitly implements the introduced interface (or it's subinterface), the implicit implementation should be overridden.
     */

    public interface ISubInterface
    {
        int SubInterfaceMethod();
    }

    public interface ISuperInterface
    {
        int SuperInterfaceMethod();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( (INamedType)TypeFactory.GetType( typeof(ISuperInterface) ) );
        }

        [Introduce]
        public int SubInterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface method." );

            return meta.Proceed();
        }

        [Introduce]
        public int SuperInterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    public class TargetClass : ISubInterface
    {
        public int SubInterfaceMethod()
        {
            Console.WriteLine( "This is original interface method." );

            return 27;
        }
    }
}