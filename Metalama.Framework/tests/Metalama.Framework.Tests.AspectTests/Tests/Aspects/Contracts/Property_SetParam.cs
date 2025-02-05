using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_SetParam
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

        public string P
        {
            get;
            [param: NotNull]
            set;
        }

        public string Q
        {
            get => q!;

            [param: NotNull]
            set => q = value + "-";
        }
    }
}