// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Fabrics.TypeFabricDiagnostics
{
    // <target>
    internal class TargetCode
    {
        public void Method1() { }

        public void Method2() { }

        private class Fabric : TypeFabric
        {
            private static readonly DiagnosticDefinition<string> _warning =
                new( "MY001", Severity.Warning, "Warning on type '{0}'." );

            public override void AmendType( ITypeAmender amender )
            {
                amender.Diagnostics.Report( _warning.WithArguments( amender.Type.Name ) );
            }
        }
    }
}
