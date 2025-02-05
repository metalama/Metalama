// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Defines a diagnostic with a strongly-typed set of parameters that are typically specified by using a named tuple for generic parameter
/// <typeparamref name="T"/>. For diagnostics that accept a single parameter, <typeparamref name="T"/> must be set to the type of this parameter.
/// To accept several parameters, use a tuple.
/// For a diagnostic that does not accept parameters, use <see cref="DiagnosticDefinition"/>. 
/// </summary>
/// <typeparam name="T">Type of arguments: a single type if there is a single argument, or a named tuple type for several arguments
/// Alternatively, you can also use <c>object[]</c>.
/// </typeparam>
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
    /// Creates an instance of the current diagnostic definition with specific arguments.
    /// </summary>
    public IDiagnostic WithArguments( T arguments ) => new DiagnosticImpl<T>( this, arguments, ImmutableArray<IDiagnosticExtension>.Empty );

    public override string ToString() => $"{this.Severity} {this.Id}: {this.Title}";
}