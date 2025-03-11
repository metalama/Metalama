// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.RemoveDiagnosticMessage;

public class TheAspect : TypeAspect
{
    public static DiagnosticDefinition MyWarning = new( "ID001", Severity.Warning, "The warning." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Diagnostics.Report( MyWarning );
    }
}

// <target>
[TheAspect]
internal class C { }