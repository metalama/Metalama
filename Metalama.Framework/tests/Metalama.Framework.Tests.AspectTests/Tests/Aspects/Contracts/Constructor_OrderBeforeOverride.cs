using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Constructor_OrderBeforeOverride;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(NotNullAttribute), typeof(NotEmptyAttribute), typeof(OverrideAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Constructor_OrderBeforeOverride
{
    internal class NotNullAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    internal class OverrideAttribute : ConstructorAspect
    {
        public override void BuildAspect( IAspectBuilder<IConstructor> builder )
        {
            builder.Override( nameof(Template) );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( "Override" );
            meta.Proceed();
        }
    }

    internal class NotEmptyAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value.Length == 0)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    internal class Target
    {
        [Override]
        public Target( [NotEmpty] [NotNull] string m ) { }
    }
}