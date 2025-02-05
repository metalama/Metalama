using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.TestApp.Aspects
{
    [Inheritable]
    class InheritedAspect : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod() { }

        public override void BuildEligibility(IEligibilityBuilder<INamedType> builder)
        {
            base.BuildEligibility(builder);
            builder.ExceptForInheritance().MustNotBeInterface();
        }
    }
}
