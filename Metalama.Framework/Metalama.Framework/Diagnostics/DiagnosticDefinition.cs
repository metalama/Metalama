// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics
{
    // ReSharper disable once UnusedTypeParameter

    /// <summary>
    /// Defines a diagnostic that does not accept any parameters. For a diagnostic that accepts parameters, use <see cref="DiagnosticDefinition{T}"/>.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    public sealed class DiagnosticDefinition : DiagnosticDefinition<None>, IDiagnostic
    {
        // Constructor used by internal code.
        internal DiagnosticDefinition( string id, string category, string messageFormat, Severity severity, string title )
            : this( id, severity, messageFormat, title, category ) { }

        // Constructor used by internal code.
        internal DiagnosticDefinition( string id, string title, string messageFormat, string category, Severity severity )
            : this( id, severity, messageFormat, title, category ) { }

        public DiagnosticDefinition( string id, Severity severity, string messageFormat, string? title = null, string? category = null ) : base(
            id,
            severity,
            messageFormat,
            title,
            category ) { }

        IDiagnosticDefinition IDiagnostic.Definition => this;

        ImmutableArray<IDiagnosticExtension> IDiagnostic.Extensions => ImmutableArray<IDiagnosticExtension>.Empty;

        object? IDiagnostic.Arguments => default(None);

        public IDiagnostic WithExtensions( ImmutableArray<IDiagnosticExtension> extensions ) => new DiagnosticImpl<None>( this, default, extensions );
    }
}