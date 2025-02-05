// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Diagnostics
{
    public readonly struct ImmutableUserDiagnosticList
    {
        public static ImmutableUserDiagnosticList Empty { get; } = new(
            ImmutableArray<Diagnostic>.Empty,
            ImmutableArray<ScopedSuppression>.Empty,
            ImmutableArray<IDiagnosticExtension>.Empty );

        internal bool IsEmpty => this.ReportedDiagnostics.IsDefaultOrEmpty && this.Extensions.IsDefaultOrEmpty && this.DiagnosticSuppressions.IsDefaultOrEmpty;

        public ImmutableArray<Diagnostic> ReportedDiagnostics { get; }

        public ImmutableArray<ScopedSuppression> DiagnosticSuppressions { get; }

        public ImmutableArray<IDiagnosticExtension> Extensions { get; }

        private ImmutableUserDiagnosticList(
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<ScopedSuppression> suppressions,
            ImmutableArray<IDiagnosticExtension> codeFixes )
        {
            this.Extensions = codeFixes.IsDefault ? ImmutableArray<IDiagnosticExtension>.Empty : codeFixes;
            this.ReportedDiagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
            this.DiagnosticSuppressions = suppressions.IsDefault ? ImmutableArray<ScopedSuppression>.Empty : suppressions;
        }

        public ImmutableUserDiagnosticList(
            ImmutableArray<Diagnostic>? diagnostics,
            ImmutableArray<ScopedSuppression>? suppressions,
            ImmutableArray<IDiagnosticExtension>? codeFixes )
            : this(
                diagnostics ?? ImmutableArray<Diagnostic>.Empty,
                suppressions ?? ImmutableArray<ScopedSuppression>.Empty,
                codeFixes ?? ImmutableArray<IDiagnosticExtension>.Empty ) { }

        // Coverage: ignore (design time)
        internal ImmutableUserDiagnosticList(
            IReadOnlyList<Diagnostic> diagnostics,
            ImmutableArray<ScopedSuppression> suppressions = default,
            ImmutableArray<IDiagnosticExtension> codeFixes = default )
            : this( diagnostics.ToImmutableArray(), suppressions, codeFixes ) { }

        internal ImmutableUserDiagnosticList Concat( in ImmutableUserDiagnosticList other )
            => new(
                this.ReportedDiagnostics.AddRange( other.ReportedDiagnostics ),
                this.DiagnosticSuppressions.AddRange( other.DiagnosticSuppressions ),
                this.Extensions.AddRange( other.Extensions ) );

        public override string ToString()
            => $"Diagnostics={this.ReportedDiagnostics.Length}, Suppressions={this.DiagnosticSuppressions.Length}, CodeFixes={this.Extensions.Length}";
    }
}