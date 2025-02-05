using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.IntegrationTests.Aspects.Applying.InvalidTarget
{
    // Intentionally not using TypeAspect so we have no AttributeUsage.
    public class IntroductionAttribute : Attribute, IAspect<INamedType>
    {
        public void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }
    }

    // <target>
    internal class TargetClass
    {
        [Introduction]
        private void Method() { }
    }
}