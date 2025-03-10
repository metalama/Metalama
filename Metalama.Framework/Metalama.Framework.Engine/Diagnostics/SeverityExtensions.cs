// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics
{
    public static class SeverityExtensions
    {
        public static DiagnosticSeverity ToRoslynSeverity( this Severity severity )
            => severity switch
            {
                Severity.Error => DiagnosticSeverity.Error,
                Severity.Hidden => DiagnosticSeverity.Hidden,
                Severity.Info => DiagnosticSeverity.Info,
                Severity.Warning => DiagnosticSeverity.Warning,
                _ => throw new AssertionFailedException( $"Unexpected Severity: {severity}." )
            };

        internal static Severity ToOurSeverity( this DiagnosticSeverity severity )
            => severity switch
            {
                DiagnosticSeverity.Error => Severity.Error,
                DiagnosticSeverity.Hidden => Severity.Hidden,
                DiagnosticSeverity.Info => Severity.Info,
                DiagnosticSeverity.Warning => Severity.Warning,
                _ => throw new AssertionFailedException( $"Unexpected Severity: {severity}." )
            };
    }
}