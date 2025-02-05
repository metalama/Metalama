using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly
{
    [Inheritable]
    public class Aspect : TypeAspect
    {
        [Introduce]
        public void Introduced() { }

        public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            base.BuildEligibility( builder );
            builder.ExceptForInheritance().MustNotBeInterface();
        }
    }

    [Aspect]
    public interface I { }

    public interface J : I { }
}