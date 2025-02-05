// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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