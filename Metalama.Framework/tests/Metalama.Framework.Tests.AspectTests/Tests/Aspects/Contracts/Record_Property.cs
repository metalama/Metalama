// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Record_Property
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

    // <target>
    internal record Target
    {
        [NotNull]
        public string M { get; set; }
    }
}