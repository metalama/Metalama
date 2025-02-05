using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0169, CS0649

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Inheritance_AutoProperty_Get
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

    internal interface ITarget
    {
        [NotNull]
        string P { get; }
    }

    // <target>
    internal class Target : ITarget
    {
        public string P { get; } = null!;

        public Target()
        {
            P = "42";
        }
    }
}