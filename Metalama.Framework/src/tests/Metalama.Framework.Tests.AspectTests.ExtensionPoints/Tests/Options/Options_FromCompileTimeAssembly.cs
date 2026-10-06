// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Options.Options_FromCompileTimeAssembly;

// The hierarchical options type is declared by the API of the test extension, which is an additional compile-time assembly and not a
// compile-time project. The engine registers it, so the value that the fabric sets on a type is inherited by its method, and the method of the
// other type gets the default value.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes().Where( t => t.Name == "Configured" ).SetOptions( new TestExtensionOptions { Value = "configured" } );
}

internal class ReportOptionsAttribute : MethodAspect
{
    private static readonly DiagnosticDefinition<string> _value = new( "MY001", Severity.Warning, "Value: {0}." );

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
        => builder.Diagnostics.Report( _value.WithArguments( builder.Target.Enhancements().GetOptions<TestExtensionOptions>().Value ?? "null" ) );
}

// <target>
internal class Configured
{
    [ReportOptions]
    public void M() { }
}

internal class NotConfigured
{
    [ReportOptions]
    public void M() { }
}
