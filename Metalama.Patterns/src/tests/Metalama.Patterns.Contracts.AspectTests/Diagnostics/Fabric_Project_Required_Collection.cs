// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable SA1649, SA1402

using System.Collections.Generic;
using Metalama.Framework.Fabrics;

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics.Fabric_Project_Required_Collection
{
    internal class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.VerifyNotNullableDeclarations();
        }
    }

    // <target>
    public class TestClass
    {
        // [Required] on collection parameter SHOULD trigger LAMA5003 because [Required]
        // and [NotNull] have identical behavior for non-string types.
        public void ProcessItems( [Required] ICollection<string> items ) { }
    }
}
