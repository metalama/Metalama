using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Eligibility.OrCondition
{
    internal class Aspect : OverrideMethodAspect
    {
        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            base.BuildEligibility( builder );

            builder.MustSatisfyAny(
                b => b.MustSatisfy( x => x.IsVirtual, x => $"{x} must be virtual" ),
                b => b.MustSatisfy( x => x.IsAbstract, x => $"{x} must be abstract" ) );
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    internal class TargetCode
    {
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}