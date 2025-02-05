using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Constructor_Order;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(NotNullAttribute), typeof(NotEmptyAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Constructor_Order
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
        public Target( [NotEmpty] [NotNull] string m ) { }
    }
}