using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Extensions.Validation;

namespace Aspects
{
    public class CanOnlyBeUsedFromAttribute : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Outbound.ValidateReferences( new Validator() );
        }
    }

    public class Validator : ReferenceValidator
    {
        public override void Validate( in ReferenceValidationContext context )
        {
        }
    }

    [Inheritable]
    public class InheritedAspectAttribute : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
        }
    }

    public class SkinableWidget
    {
        protected class Internals
        {
            [CanOnlyBeUsedFrom]
            internal static void StaticMethodWithReferentialConstraint()
            {
            }

            [InheritedAspect]
            internal virtual void VirtualMethodWithInheritedAspect()
            {
            }
        }
    }
}