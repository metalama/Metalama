using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS0618, CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_InvalidDirections
{
    internal class NotNullAttribute : ContractAspect
    {
        public NotNullAttribute( ContractDirection direction ) : base( direction ) { }

        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    internal class Target
    {
        // All these situations are invalid and should result in eligibility errors.

        private void M1( [NotNull( ContractDirection.Output )] string m ) { }

        private void M2( [NotNull( ContractDirection.Both )] string m ) { }

        private void M3( [NotNull( ContractDirection.Input )] out string m )
        {
            m = "";
        }

        private void M4( [NotNull( ContractDirection.Both )] out string m )
        {
            m = "";
        }
    }
}