// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

public class MyAspect : OverrideMethodAspect
{
    private static readonly DiagnosticDefinition _diagnostic = new("MY001", Severity.Warning, "Some aspect warning.");

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );
        builder.Diagnostics.Report( _diagnostic );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("Overridden.");
        return meta.Proceed();

    }
}