// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics
{
    // ReSharper disable once UnusedTypeParameter

    /// <summary>
    /// Defines a diagnostic that does not accept any parameters. For diagnostics with parameters, use <see cref="DiagnosticDefinition{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diagnostic definitions must be declared as static fields in compile-time classes (such as aspect classes,
    /// fabric classes, or other compile-time helper classes) and include a unique ID, severity level, and message
    /// format string. The aspect framework requires diagnostics to be defined this way to ensure they can be
    /// properly detected by static analysis of the code.
    /// </para>
    /// <para>
    /// This class implements both <see cref="IDiagnosticDefinition"/> and <see cref="IDiagnostic"/>, which means
    /// instances can be used directly with <see cref="ScopedDiagnosticSink.Report(IDiagnostic)"/> without needing
    /// to call <c>WithArguments</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// private static readonly DiagnosticDefinition _missingFieldError = new(
    ///     "MY001",
    ///     Severity.Error,
    ///     "The type must have a field named '_logger'.");
    ///
    /// // In BuildAspect:
    /// builder.Diagnostics.Report(_missingFieldError);
    /// </code>
    /// </example>
    /// <seealso cref="DiagnosticDefinition{T}"/>
    /// <seealso cref="IDiagnosticDefinition"/>
    /// <seealso cref="IDiagnostic"/>
    /// <seealso cref="Severity"/>
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