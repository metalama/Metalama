#if TEST_OPTIONS
// @Include(_AdviceResultShared.cs)
# endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using Metalama.Framework.Engine.Advising;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AdviceResult_Override_ImplementedBase
{
    /*
     * Tests that advice result with members with ignore override strategy contains correct values.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            var result = aspectBuilder.ImplementInterface( typeof(IInterface), OverrideStrategy.Override );

            if (result.Outcome != AdviceOutcome.Default)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Default." );
            }

            if (result.AdviceKind != AdviceKind.ImplementInterface)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of ImplementInterface." );
            }

            aspectBuilder.Advice.WithTemplateProvider( new AdviceResultTemplates() )
                .Override(
                    aspectBuilder.Target.Methods.OfName( "Witness" ).Single(),
                    nameof(AdviceResultTemplates.WitnessTemplate),
                    args: new { types = result.Interfaces, members = result.GetObsoleteInterfaceMembers() } );
        }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Default )]
        public void BaseMethod() { }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Default )]
        public int BaseProperty { get; set; }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Default )]
        public event EventHandler? BaseEvent;

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Default )]
        public void Method() { }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Default )]
        public int Property { get; set; }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Default )]
        public event EventHandler? Event;
    }

    // <target>
    [Introduction]
    public class TargetClass : IBaseInterface
    {
        public void BaseMethod() { }

        public int BaseProperty { get; set; }

        public event EventHandler? BaseEvent;

        public void Method() { }

        public int Property { get; set; }

        public event EventHandler? Event;

        public void Witness() { }
    }
}