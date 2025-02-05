using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.PublicPipeline.Aspects.Eligibility.Programmatic;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1) )]

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Eligibility.Programmatic
{
    internal class Aspect1 : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            base.BuildAspect( builder );

            builder.Outbound.SelectMany( t => t.Methods ).AddAspect( x => new Aspect2() );
        }
    }

    internal class Aspect2 : OverrideMethodAspect
    {
        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            base.BuildEligibility( builder );
            builder.MustNotHaveRefOrOutParameter();
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    [Aspect1]
    internal class TargetCode
    {
        private int Method( out int a )
        {
            a = 0;

            return a;
        }
    }
}