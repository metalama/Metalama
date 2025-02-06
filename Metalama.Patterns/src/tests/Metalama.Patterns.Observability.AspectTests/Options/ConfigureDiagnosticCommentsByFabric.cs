// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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