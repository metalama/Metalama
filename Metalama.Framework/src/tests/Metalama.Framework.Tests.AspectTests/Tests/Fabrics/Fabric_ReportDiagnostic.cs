// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;

#pragma warning disable CS0168, CS8618, CS0169

namespace Metalama.Framework.Tests.AspectTests.Validation.Fabric_ReportDiagnostic
{
    internal class MyFabric : ProjectFabric
    {
        private static readonly DiagnosticDefinition<IDeclaration> _warning =
            new( "MY001", Severity.Warning, "Warning on {0}." );

        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectMany( x => x.Types.SelectMany( t => t.Methods ) ).ReportDiagnostic( t => _warning.WithArguments( t ) );
        }
    }

    // <target>
    internal class ValidatedClass
    {
        public static void Method1( object o ) { }

        public static void Method2( object o ) { }
    }
}