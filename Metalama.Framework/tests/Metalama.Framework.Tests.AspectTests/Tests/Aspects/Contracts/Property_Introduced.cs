using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618, CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_Introduced
{
    /*
     * Tests that filter works on introduced property within the same aspect.
     */

    internal class IntroduceAndFilterAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var property in builder.Target.Properties)
            {
                builder.With( property ).AddContract( nameof(Filter), ContractDirection.Both );
            }

            var introducedField = builder.IntroduceProperty( nameof(IntroducedProperty) ).Declaration;

            builder.With( introducedField ).AddContract( nameof(Filter), ContractDirection.Both );
        }

        [Template]
        public string? IntroducedProperty { get; set; }

        [Template]
        public void Filter( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
        }
    }

    // <target>
    [IntroduceAndFilter]
    internal class Target
    {
        public string? ExistingProperty { get; set; }
    }
}