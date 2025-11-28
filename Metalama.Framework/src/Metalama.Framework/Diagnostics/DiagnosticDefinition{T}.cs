// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Defines a diagnostic with strongly-typed parameters.
/// </summary>
/// <remarks>
/// <para>
/// Diagnostic definitions must be declared as static fields in compile-time classes (such as aspect classes,
/// fabric classes, or other compile-time helper classes). They specify a unique ID, severity level, and a
/// message format string that uses the standard .NET string formatting syntax.
/// </para>
/// <para>
/// To report a diagnostic with parameters, first call <see cref="WithArguments"/> to create an <see cref="IDiagnostic"/>
/// instance with the parameter values, then pass it to <see cref="ScopedDiagnosticSink.Report(IDiagnostic)"/>.
/// </para>
/// <para>
/// For a single parameter, set <typeparamref name="T"/> to the parameter type (e.g., <c>DiagnosticDefinition&lt;string&gt;</c>).
/// For multiple parameters, use a tuple (e.g., <c>DiagnosticDefinition&lt;(int, string)&gt;</c>).
/// For weakly-typed parameters, use <c>DiagnosticDefinition&lt;object[]&gt;</c>.
/// For diagnostics without parameters, use <see cref="DiagnosticDefinition"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// private static readonly DiagnosticDefinition&lt;string&gt; _fieldNotFoundError = new(
///     "MY002",
///     Severity.Error,
///     "The field '{0}' was not found in type.");
///
/// // In BuildAspect:
/// builder.Diagnostics.Report(_fieldNotFoundError.WithArguments("_logger"));
/// </code>
/// </example>
/// <typeparam name="T">The type of arguments: a single type for one parameter, or a tuple type for multiple parameters.
/// Alternatively, you can use <c>object[]</c> for weakly-typed parameters.</typeparam>
/// <seealso cref="DiagnosticDefinition"/>
/// <seealso cref="IDiagnosticDefinition"/>
/// <seealso cref="IDiagnostic"/>
/// <seealso cref="Severity"/>
/// <seealso href="@diagnostics"/>
public class DiagnosticDefinition<T> : IDiagnosticDefinition
    where T : notnull
{
    // Constructor used by internal code.
    internal DiagnosticDefinition( string id, string category, string messageFormat, Severity severity, string title )
        : this( id, severity, messageFormat, title, category ) { }

    // Constructor used by internal code.
    internal DiagnosticDefinition( string id, string title, string messageFormat, string category, Severity severity )
        : this( id, severity, messageFormat, title, category ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticDefinition{T}"/> class.
    /// </summary>
    /// <param name="id">An unique identifier for the diagnostic (e.g. <c>MY001</c>).</param>
    /// <param name="severity">The severity of the diagnostic.</param>
    /// <param name="messageFormat">The formatting string of the diagnostic message.</param>
    /// <param name="title">An optional short title for the diagnostic. If no value is provided for this parameter, <paramref name="messageFormat"/> is used.</param>
    /// <param name="category">An optional category to which this diagnostic belong. The default value is <c>Metalama.User</c>.</param>
    public DiagnosticDefinition( string id, Severity severity, string messageFormat, string? title = null, string? category = null )
    {
        this.Severity = severity;
        this.Id = id;
        this.MessageFormat = messageFormat;
        this.Title = title ?? messageFormat;
        this.Category = category ?? "Metalama.User";
    }

    /// <inheritdoc />
    public Severity Severity { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string MessageFormat { get; }

    /// <inheritdoc />
    public string Category { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <summary>
    /// Creates a diagnostic instance with the specified arguments.
    /// </summary>
    /// <param name="arguments">The arguments to be formatted into the diagnostic message. For multiple parameters, pass a tuple.</param>
    /// <returns>An <see cref="IDiagnostic"/> instance that can be reported using <see cref="ScopedDiagnosticSink.Report(IDiagnostic)"/>.</returns>
    public IDiagnostic WithArguments( T arguments ) => new DiagnosticImpl<T>( this, arguments, ImmutableArray<IDiagnosticExtension>.Empty );

    public override string ToString() => $"{this.Severity} {this.Id}: {this.Title}";
}