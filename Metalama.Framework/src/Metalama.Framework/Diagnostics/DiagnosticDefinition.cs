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
        internal DiagnosticDefinition( string id, string category, string messageFormat, Severity severity, string title, string? helpLinkUri = null )
            : this( id, severity, messageFormat, title, category, helpLinkUri ) { }

        // Constructor used by internal code.
        internal DiagnosticDefinition( string id, string title, string messageFormat, string category, Severity severity, string? helpLinkUri = null )
            : this( id, severity, messageFormat, title, category, helpLinkUri ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticDefinition"/> class.
        /// </summary>
        /// <param name="id">An unique identifier for the diagnostic (e.g. <c>MY001</c>).</param>
        /// <param name="severity">The severity of the diagnostic.</param>
        /// <param name="messageFormat">The formatting string of the diagnostic message.</param>
        /// <param name="title">An optional short title for the diagnostic. If no value is provided for this parameter, <paramref name="messageFormat"/> is used.</param>
        /// <param name="category">An optional category to which this diagnostic belong. The default value is <c>Metalama.User</c>.</param>
        public DiagnosticDefinition( string id, Severity severity, string messageFormat, string? title = null, string? category = null ) : base(
            id,
            severity,
            messageFormat,
            title,
            category ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticDefinition"/> class and specifies the URL of the documentation page of the diagnostic.
        /// </summary>
        /// <param name="id">An unique identifier for the diagnostic (e.g. <c>MY001</c>).</param>
        /// <param name="severity">The severity of the diagnostic.</param>
        /// <param name="messageFormat">The formatting string of the diagnostic message.</param>
        /// <param name="title">A short title for the diagnostic. If the value is <c>null</c>, <paramref name="messageFormat"/> is used.</param>
        /// <param name="category">The category to which this diagnostic belong. If the value is <c>null</c>, <c>Metalama.User</c> is used.</param>
        /// <param name="helpLinkUri">The URL of the documentation page of the diagnostic, or <c>null</c>. If the URL contains the <c>{0}</c> placeholder,
        /// the placeholder is replaced by the lower-case value of <paramref name="id"/>. For instance, <c>https://example.com/errors/{0}</c> becomes
        /// <c>https://example.com/errors/my001</c>.</param>
        public DiagnosticDefinition( string id, Severity severity, string messageFormat, string? title, string? category, string? helpLinkUri ) : base(
            id,
            severity,
            messageFormat,
            title,
            category,
            helpLinkUri ) { }

        IDiagnosticDefinition IDiagnostic.Definition => this;

        ImmutableArray<IDiagnosticExtension> IDiagnostic.Extensions => ImmutableArray<IDiagnosticExtension>.Empty;

        object? IDiagnostic.Arguments => default(None);

        public IDiagnostic WithExtensions( ImmutableArray<IDiagnosticExtension> extensions ) => new DiagnosticImpl<None>( this, default, extensions );
    }
}