using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_Set
{
    internal class NotNullAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
        }
    }

    // <target>
    internal class Target
    {
        private string? q;

        [NotNull]
        public string Q
        {
            set => q = value + "-";
        }
    }
}