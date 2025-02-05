using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

#pragma warning disable 618

namespace Metalama.Framework.TestApp.Aspects.Validation
{
    // This file is a demo use of aspect markers and template subroutines.

    public abstract class ValidateAttribute : Attribute, IAspect<IParameter>, IAspect<IFieldOrProperty>
    {
        public virtual void BuildEligibility(IEligibilityBuilder<IParameter> builder) 
        {
            builder.Require(p => p.DeclaringMember is IMethod, p => $"the declaring member of the parameter must be a method");
        }


        public void BuildEligibility(IEligibilityBuilder<IFieldOrProperty> builder)
        {
            builder.Require(x => x.IsReadOnly, x => $"{x} cannot be read-only");
        }

        public virtual int Priority { get; }

        [Template]
        public abstract void Validate(string name, dynamic value);

        public void BuildAspect(IAspectBuilder<IParameter> builder)
        {
            builder.RequireAspect<IMethod,ValidateAspect>((IMethod) builder.TargetDeclaration.DeclaringMember);
        }

        public void BuildAspect(IAspectBuilder<IFieldOrProperty> builder)
        {
            builder.RequireAspect<IFieldOrProperty, ValidateAspect>(builder.TargetDeclaration);
        }

    }
}
