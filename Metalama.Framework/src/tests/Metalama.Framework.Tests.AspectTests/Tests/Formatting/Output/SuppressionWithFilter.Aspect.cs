// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Output.SuppressionWithFilter;

public class SuppressFilteredAttribute : TypeAspect
{
    private static readonly SuppressionDefinition _suppression = new("CS0414");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        // This suppression matches CS0414 but only for fields named "_initialized"
        builder.Diagnostics.Suppress(
            _suppression.WithFilter(
                diag => diag.Arguments.Any(arg => arg is string name && name == "C._initialized")),
            builder.Target);
    }
}
