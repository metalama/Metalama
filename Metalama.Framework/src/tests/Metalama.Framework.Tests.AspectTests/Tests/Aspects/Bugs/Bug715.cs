// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug715;

/*
 * Tests that diagnostic messages with multi-line arguments have their newlines
 * escaped to spaces, so the message is not cut off.
 * This is a regression test for https://github.com/metalama/Metalama/issues/715.
 */

public class Aspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _warning =
        new( "BUG715", Severity.Warning, "The statement '{0}' is multi-line." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Diagnostics.Report(
            _warning.WithArguments( "bool CanExecute(object? parameter)\n{\n    return true;\n}" ) );
    }
}

// <target>
[Aspect]
internal class Target;
