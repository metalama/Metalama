using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Record_InterfaceProperty
{
    internal class NotNullAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Property.Name );
            }
        }
    }

    public interface I
    {
        string? M { get; set; }

        string? N { get; set; }
    }

    // <target>
    internal record Target : I
    {
        [NotNull]
        string? I.M { get; set; }

        [NotNull]
        string? I.N { get; set; } = default;
    }
}