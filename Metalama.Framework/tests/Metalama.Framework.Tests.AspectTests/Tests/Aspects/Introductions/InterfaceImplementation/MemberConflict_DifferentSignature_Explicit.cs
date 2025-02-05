using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_DifferentSignature_Explicit
{
    /*
     * Tests that when a member of the same name but different signature already exists in the target class and the interface member is explicit, the compilation succeeds.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder
                .ImplementInterface(
                    typeof(IInterface),
                    whenExists: OverrideStrategy.Ignore );
        }

        [InterfaceMember( IsExplicit = true )]
        public int Method()
        {
            Console.WriteLine( "This is introduced interface method." );

            return 42;
        }
    }

    public interface IInterface
    {
        int Method();
    }

    // <target>
    [Introduction]
    public class TargetClass
    {
        public int Method( int x )
        {
            Console.WriteLine( "This is original method." );

            return x;
        }
    }
}