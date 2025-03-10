// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS0169, CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_InvalidDirections
{
    internal class NotNullAttribute : ContractAspect
    {
        public NotNullAttribute( ContractDirection direction ) : base( direction ) { }

        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
        }
    }

    internal class Target
    {
        private string q;

        // All these targets are invalid.

        [NotNull( ContractDirection.Input )]
        public string P1 => "";

        [NotNull( ContractDirection.Both )]
        public string P2 => "";

        [NotNull( ContractDirection.Output )]
        public string P3
        {
            set { }
        }
    }
}