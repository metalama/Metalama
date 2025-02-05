using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Eligibility.Static
{
    internal class Aspect : OverrideMethodAspect
    {
        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            base.BuildEligibility( builder );
            builder.MustBeStatic();
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