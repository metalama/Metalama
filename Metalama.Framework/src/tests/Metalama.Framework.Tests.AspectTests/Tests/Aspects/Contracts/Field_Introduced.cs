// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Field_Introduced;

#pragma warning disable CS8618, CS0169, CS0649

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(IntroduceAndFilterAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Field_Introduced
{
    /*
     * Tests that filter works on introduced field within the same aspect.
     */

    internal class IntroduceAndFilterAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var field in builder.Target.Fields)
            {
                builder.With( field ).AddContract( nameof(Filter), ContractDirection.Both );
            }

            var introducedField = builder.IntroduceField( nameof(IntroducedField) ).Declaration;

            builder.With( introducedField ).AddContract( nameof(Filter), ContractDirection.Both );
        }

        [Template]
        private string? IntroducedField;

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
        private string ExistingField;
    }
}