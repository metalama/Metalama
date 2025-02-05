using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.TestApp.Aspects.Validation
{

    internal class ValidateAspect : IAspect<IMethod>, IAspect<IFieldOrProperty>
    {
        [Template]
        private dynamic ValidateMethod()
        {
            var typedMarkers = meta.UpstreamAspects
                .Select(m => (Parameter: (IParameter)m.TargetDeclaration, Marker: (ValidateAttribute)m.Aspect))
                .ToList();

            var markersOnParameters = typedMarkers
                .Where(m => m.Parameter.Index >= 0)
                .OrderBy(m => m.Parameter.Index)
                .ThenBy(m => m.Marker.Priority);

            var markersOnReturnValue = typedMarkers.Where(m => m.Parameter.Index < 0);

            foreach (var marker in markersOnParameters)
            {
                var adviceParameter = meta.Parameters[marker.Parameter.Index];
                marker.Marker.Validate(marker.Parameter.Name, adviceParameter.Value);
            }

            var returnValue = meta.Proceed();

            foreach (var marker in markersOnReturnValue)
            {
                marker.Marker.Validate("return value", returnValue);
            }

            return returnValue;
        }

        [Template]
        private void ValidateDynamic(object value)
        {
            dynamic castValue = meta.FieldOrProperty.Type.Cast(value);

            foreach (var marker in meta.UpstreamAspects)
            {
                ((ValidateAttribute)marker.Aspect).Validate(meta.FieldOrProperty.Name, castValue);
            }
        }

        [Template]
        private void ValidateFieldOrPropertySetter()
        {
         
            foreach (var marker in meta.UpstreamAspects)
            {
                ((ValidateAttribute)marker.Aspect).Validate(meta.FieldOrProperty.Name, meta.FieldOrProperty.GetValue(meta.This));
            }

            meta.Proceed();

        }

        public void BuildAspect(IAspectBuilder<IMethod> builder)
        {
            builder.AdviceFactory.OverrideMethod(builder.TargetDeclaration, nameof(this.ValidateMethod));
        }

        public void BuildAspect(IAspectBuilder<IFieldOrProperty> builder)
        {
            // if not dependency property
            // {
            builder.AdviceFactory.OverrideFieldOrPropertyAccessors(builder.TargetDeclaration, null, nameof(this.ValidateFieldOrPropertySetter));

            // } else {
            var method = builder.AdviceFactory.IntroduceMethod(builder.TargetDeclaration.DeclaringType, nameof(this.ValidateDynamic));
            method.Name = "Validate_" + builder.TargetDeclaration.Name;

            // the dependency property advice would be supposed to use the method Validate_Property if it exists.
            // }
        }

        public void BuildEligibility(IEligibilityBuilder<IMethod> method)
        {
            method.MustBeNonAbstract();
        }

        public void BuildEligibility(IEligibilityBuilder<IFieldOrProperty> member)
        {
            member.MustBeNonAbstract();
        }
    }
}
