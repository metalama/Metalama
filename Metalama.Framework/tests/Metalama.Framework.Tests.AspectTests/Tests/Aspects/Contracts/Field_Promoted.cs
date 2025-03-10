// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Field_Promoted;

#pragma warning disable CS8618, CS0169, CS0649

[assembly: AspectOrder(AspectOrderDirection.RunTime, typeof(IntroduceAndFilterAttribute))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Field_Promoted
{
    /*
     * Tests that contract works on promoted field.
     */

    internal class IntroduceAndFilterAttribute : TypeAspect
    {
        public override void BuildAspect(IAspectBuilder<INamedType> builder)
        {
            var field = builder.Target.Fields.Single();

            builder.Advice.Override(field, nameof(Template));
            builder.With(field).AddContract(nameof(Contract), ContractDirection.Both);
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                return meta.Proceed();
            }
            set
            {
                meta.Proceed();
            }
        }

        [Template]
        public void Contract(dynamic? value)
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