// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable SA1649, SA1402

using Metalama.Framework.Fabrics;

namespace Metalama.Patterns.Contracts.AspectTests.Fabric_Project_Required_String
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
        // [Required] on string parameter should NOT trigger LAMA5003 because [Required]
        // checks for empty/whitespace strings, which [NotNull] does not.
        public void PrintString( [Required] string foo )
        {
        }

        // [Required] on string property should also be silently skipped.
        [Required]
        public string Name { get; set; }

        // [Required] on string field.
        [Required]
        public string Title;
    }
}
