// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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