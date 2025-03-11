// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(DEBUG)
#endif

#if DEBUG
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability.AspectTests.ConfigureDiagnosticCommentsByFabric;

public class Fabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.ConfigureObservability( b => b.DiagnosticCommentVerbosity = 1 );
    }
}

// <target>
[Observable]
public class ConfigureDiagnosticCommentsByFabric { }

#endif