#if TEST_OPTIONS
// @Include(_AdviceResultShared.cs)
# endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AdviceResult_Override_MembersFail
{
    /*
     * Tests that advice result with members with ignore override strategy contains correct values.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            var result = aspectBuilder.ImplementInterface( typeof(IInterface), OverrideStrategy.Override );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
            }

            if (result.AdviceKind != AdviceKind.ImplementInterface)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of ImplementInterface." );
            }
        }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public void BaseMethod() { }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public int BaseProperty { get; set; }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public event EventHandler? BaseEvent;

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public void Method() { }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public int Property { get; set; }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public event EventHandler? Event;
    }

    // <target>
    [Introduction]
    public class TargetClass
    {
        public void BaseMethod() { }

        public int BaseProperty { get; set; }

        public event EventHandler? BaseEvent;

        public void Method() { }

        public int Property { get; set; }

        public event EventHandler? Event;
    }
}