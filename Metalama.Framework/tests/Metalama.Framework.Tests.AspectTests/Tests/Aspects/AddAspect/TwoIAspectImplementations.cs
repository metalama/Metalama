using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.TwoIAspectImplementations
{
    public class LogAttribute : Aspect, IAspect<IMethod>, IAspect<IFieldOrProperty>
    {
        public void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(OverrideMethod) );
        }

        public void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(OverrideProperty) );
        }

        [Template]
        private dynamic? OverrideMethod()
        {
            Console.WriteLine( "Entering " + meta.Target.Method.ToDisplayString() );

            try
            {
                return meta.Proceed();
            }
            finally
            {
                Console.WriteLine( "Leaving " + meta.Target.Method.ToDisplayString() );
            }
        }

        [Template]
        private dynamic? OverrideProperty
        {
            get => meta.Proceed();

            set
            {
                Console.WriteLine( "Assigning " + meta.Target.Method.ToDisplayString() );
                meta.Proceed();
            }
        }

        public void BuildEligibility( IEligibilityBuilder<IMethod> builder ) { }

        public void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> builder ) { }
    }

    // <target>
    internal class TargetCode
    {
        [Log]
        public int Method( int a, int b )
        {
            return a + b;
        }

        [Log]
        public int Property { get; set; }

        [Log]
        public string? Field { get; set; }
    }
}