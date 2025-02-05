// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Collections;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Diagnostics;

public static class DiagnosticDescriptorExtensions
{
    public static DiagnosticDescriptor ToRoslynDescriptor( this IDiagnosticDefinition definition )
        => new( definition.Id, definition.Title, definition.MessageFormat, definition.Category, definition.Severity.ToRoslynSeverity(), true );

    /// <summary>
    /// Creates an <see cref="DiagnosticException"/> instance based on the current descriptor and given arguments.
    /// The diagnostic location will be resolved from the call stack.
    /// </summary>
    internal static Exception CreateException<T>( this DiagnosticDefinition<T> definition, T arguments )
        where T : notnull
        => new DiagnosticException( definition.CreateRoslynDiagnostic( null, arguments ) );

    /// <summary>
    /// Instantiates a <see cref="Diagnostic"/> based on the current descriptor and given arguments.
    /// </summary>
    public static Diagnostic CreateRoslynDiagnostic<T>(
        this DiagnosticDefinition<T> definition,
        Location? location,
        T arguments,
        IEnumerable<Location>? additionalLocations = null,
        string? deduplicationKey = null,
        ImmutableDictionary<string, string?>? properties = null )
        where T : notnull
    {
        // ConvertDiagnosticArguments treats an array as multiple arguments, so we need to wrap it in another array.
        var argumentArray = ConvertDiagnosticArguments( typeof(T).IsArray ? new[] { arguments } : arguments );

        return definition.CreateRoslynDiagnosticImpl( location, argumentArray, null, additionalLocations, deduplicationKey, properties );
    }

    /// <summary>
    /// Instantiates a <see cref="Diagnostic"/> based on the current descriptor and given arguments and specifies the <see cref="IDiagnosticSource"/>.
    /// </summary>
    internal static Diagnostic CreateRoslynDiagnostic<T>(
        this DiagnosticDefinition<T> definition,
        Location? location,
        T arguments,
        IDiagnosticSource? diagnosticSource,
        IEnumerable<Location>? additionalLocations = null,
        string? deduplicationKey = null,
        ImmutableDictionary<string, string?>? properties = null )
        where T : notnull
    {
        // ConvertDiagnosticArguments treats an array as multiple arguments, so we need to wrap it in another array.
        var argumentArray = ConvertDiagnosticArguments( typeof(T).IsArray ? new[] { arguments } : arguments );

        return definition.CreateRoslynDiagnosticImpl( location, argumentArray, diagnosticSource, additionalLocations, deduplicationKey, properties );
    }

    // If this was named CreateRoslynDiagnostic, type safety of the generic versions would be lost.
    internal static Diagnostic CreateRoslynDiagnosticNonGeneric(
        this IDiagnosticDefinition definition,
        Location? location,
        object? arguments,
        IDiagnosticSource? diagnosticSource = null,
        IEnumerable<Location>? additionalLocations = null,
        string? deduplicationKey = null,
        ImmutableDictionary<string, string?>? properties = null )
    {
        var argumentArray = ConvertDiagnosticArguments( arguments );

        return definition.CreateRoslynDiagnosticImpl( location, argumentArray, diagnosticSource, additionalLocations, deduplicationKey, properties );
    }

    private static object?[] ConvertDiagnosticArguments( object? arguments )
    {
        object?[] argumentArray;

        if ( arguments == null )
        {
            return [];
        }

        if ( arguments.GetType().Name.StartsWith( nameof(ValueTuple), StringComparison.OrdinalIgnoreCase ) )
        {
            argumentArray = ValueTupleAdapter.ToArray( arguments );
        }
        else if ( arguments.GetType().IsArray )
        {
            argumentArray = (object[]) arguments;
        }
        else
        {
            argumentArray = [arguments];
        }

        return argumentArray;
    }

    private static Diagnostic CreateRoslynDiagnosticImpl(
        this IDiagnosticDefinition definition,
        Location? location,
        object?[] arguments,
        IDiagnosticSource? diagnosticSource,
        IEnumerable<Location>? additionalLocations,
        string? deduplicationKey,
        ImmutableDictionary<string, string?>? properties )
    {
        var propertiesWithAdditions = properties;

        if ( deduplicationKey != null )
        {
            ImmutableDictionaryExtensions.AddOrCreate( ref propertiesWithAdditions, UserDiagnosticSink.DeduplicationPropertyKey, deduplicationKey );
        }

        return Diagnostic.Create(
            definition.Id,
            definition.Category,
            new NonLocalizedString( definition.MessageFormat, arguments ),
            definition.Severity.ToRoslynSeverity(),
            definition.Severity.ToRoslynSeverity(),
            true,
            definition.Severity == Severity.Error ? 0 : 1,
            new NonLocalizedString( definition.Title, arguments ),
            location: location,
            additionalLocations: additionalLocations,
            properties: propertiesWithAdditions,
            description: diagnosticSource == null ? null : $"Reported by {diagnosticSource.DiagnosticSourceDescription}." );
    }
}