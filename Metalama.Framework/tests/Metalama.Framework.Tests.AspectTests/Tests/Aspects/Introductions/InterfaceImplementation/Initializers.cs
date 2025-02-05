using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067, CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Initializers
{
    /*
     * Tests that initializers are copied from interface member templates.
     */

    public class IntroduceAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember]
        public int AutoProperty { get; set; } = 42;

        [InterfaceMember]
        public event EventHandler? EventField = default;
    }

    public interface IInterface
    {
        int AutoProperty { get; set; }

        event EventHandler? EventField;
    }

    // <target>
    [IntroduceAspect]
    public class TargetClass { }
}