// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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