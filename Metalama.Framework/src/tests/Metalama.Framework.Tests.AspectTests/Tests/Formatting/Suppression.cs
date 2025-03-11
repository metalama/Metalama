// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

#if !TESTRUNNER // Disable the warning in the main build, not during tests.
#pragma warning disable CS0414
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Suppression;

[Suppress]
public class C
{
    // This normally reports CS0414 but it should not be written to HTML because we suppressed it.
    private int _f = 0;
    
}

public class Suppress : TypeAspect
{
    private static SuppressionDefinition _suppression = new( "CS0414" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Diagnostics.Suppress( _suppression );
    }
}