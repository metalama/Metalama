// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Diagnostics;

[MyAspect]
public class Diagnostics
{
#if TESTRUNNER
    // C# warning: unused field.
    private int _unusedField = 0;
    
    // C# error: invalid type: 
    private ErrorType _field;

#endif
}

public class MyAspect : TypeAspect
{
    private static DiagnosticDefinition _diagnostic = new( "MY001", Severity.Warning, "Some custom warning." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.Diagnostics.Report( _diagnostic );
    }

    [Introduce]
    private void IntroducedMethod() { }
}