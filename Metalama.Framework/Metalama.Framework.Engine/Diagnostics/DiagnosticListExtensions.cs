// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Diagnostics
{
    internal static class DiagnosticListExtensions
    {
        public static bool HasError( this IEnumerable<Diagnostic> diagnosticList ) => diagnosticList.Any( d => d.Severity == DiagnosticSeverity.Error );
    }
}