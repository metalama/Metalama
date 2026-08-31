// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
        ImmutableDictionary<string, string?>? properties = null,
        string? description = null )
        where T : notnull
    {
        // ConvertDiagnosticArguments treats an array as multiple arguments, so we need to wrap it in another array.
        var argumentArray = ConvertDiagnosticArguments( typeof(T).IsArray ? new[] { arguments } : arguments );

        return definition.CreateRoslynDiagnosticImpl( location, argumentArray, null, additionalLocations, deduplicationKey, properties, description );
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
        ImmutableDictionary<string, string?>? properties = null,
        string? description = null )
        where T : notnull
    {
        // ConvertDiagnosticArguments treats an array as multiple arguments, so we need to wrap it in another array.
        var argumentArray = ConvertDiagnosticArguments( typeof(T).IsArray ? new[] { arguments } : arguments );

        return definition.CreateRoslynDiagnosticImpl(
            location,
            argumentArray,
            diagnosticSource,
            additionalLocations,
            deduplicationKey,
            properties,
            description );
    }

    // If this was named CreateRoslynDiagnostic, type safety of the generic versions would be lost.
    internal static Diagnostic CreateRoslynDiagnosticNonGeneric(
        this IDiagnosticDefinition definition,
        Location? location,
        object? arguments,
        IDiagnosticSource? diagnosticSource = null,
        IEnumerable<Location>? additionalLocations = null,
        string? deduplicationKey = null,
        ImmutableDictionary<string, string?>? properties = null,
        string? description = null )
    {
        var argumentArray = ConvertDiagnosticArguments( arguments );

        return definition.CreateRoslynDiagnosticImpl(
            location,
            argumentArray,
            diagnosticSource,
            additionalLocations,
            deduplicationKey,
            properties,
            description );
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
        ImmutableDictionary<string, string?>? properties,
        string? description = null )
    {
        var propertiesWithAdditions = properties;

        if ( deduplicationKey != null )
        {
            ImmutableDictionaryExtensions.AddOrCreate( ref propertiesWithAdditions, UserDiagnosticSink.DeduplicationPropertyKey, deduplicationKey );
        }

        var diagnosticSourceDescription = diagnosticSource == null ? null : $"Reported by {diagnosticSource.DiagnosticSourceDescription}.";
        var effectiveDescription = description ?? diagnosticSourceDescription;

        var durableArguments = MaterializeCompilationBoundArguments( arguments );

        return Diagnostic.Create(
            definition.Id,
            definition.Category,
            new NonLocalizedString( definition.MessageFormat, durableArguments ),
            definition.Severity.ToRoslynSeverity(),
            definition.Severity.ToRoslynSeverity(),
            true,
            definition.Severity == Severity.Error ? 0 : 1,
            new NonLocalizedString( definition.Title, durableArguments ),
            location: location,
            additionalLocations: additionalLocations,
            properties: propertiesWithAdditions,
            description: effectiveDescription );
    }

    /// <summary>
    /// Replaces every argument that is bound to a compilation with the string it would have been formatted to,
    /// leaving the others untouched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Diagnostic"/> formats its message lazily, so the arguments are held by the
    /// <see cref="NonLocalizedString"/> for as long as the diagnostic itself. At design time a diagnostic outlives by
    /// far the version of the project it was reported on: it is kept in the <c>SyntaxTreePipelineResult</c> of its
    /// file, and that result is carried forward to every subsequent version in which the file is not re-analysed. An
    /// argument that is a declaration therefore keeps the whole compilation of the run that reported the diagnostic
    /// alive, which is the retention described in issue #1799. Passing the declaration is the natural way to write
    /// the message, so the fix belongs here rather than in every diagnostic definition.
    /// </para>
    /// <para>
    /// Only the compilation-bound arguments are materialized, not all of them, because
    /// <see cref="MetalamaStringFormatter"/> passes the format specifier of the composite format string to an
    /// <see cref="IFormattable"/> argument. Materializing such an argument would apply its specifier to a string
    /// instead, changing the result of <c>{0:N2}</c> and making <c>{0:x}</c> throw. The two sets do not overlap: every
    /// branch of the formatter that handles a compilation-bound value formats it without regard to the specifier, so
    /// materializing exactly those arguments cannot change any message.
    /// </para>
    /// <para>
    /// Formatting here is not extra work in the ordinary case, in which the message is displayed and would have been
    /// formatted anyway, and it is done while the compilation is certainly available rather than at an arbitrary later
    /// point. The title and the message share the result instead of formatting the same arguments twice.
    /// </para>
    /// <para>
    /// The cost was measured, because it is paid on the compile-time path as well, where nothing outlives the run and
    /// the work would be wasted for a diagnostic that is created and never displayed. Creating a diagnostic whose
    /// argument is a declaration takes about 3.2 microseconds, of which about 2.3 are the display string; the same
    /// diagnostic with a string argument takes about 0.25. A build that created ten thousand such diagnostics without
    /// displaying any of them would therefore spend about twenty milliseconds more. That is small enough not to warrant
    /// making the behaviour conditional on the execution scenario, which this method has no way of reading: it is a
    /// static extension, reached from many call sites, and the project forbids passing a service as a parameter to
    /// obtain one.
    /// </para>
    /// </remarks>
    private static object?[] MaterializeCompilationBoundArguments( object?[] arguments )
    {
        if ( arguments.Length == 0 )
        {
            return arguments;
        }

        // A caller that formats ahead of time must not fail when no formatter has been registered, because leaving the
        // arguments alone is a correct outcome: they are then formatted later, exactly as before this optimization.
        var formatter = MetalamaStringFormatter.InstanceOrNull;

        if ( formatter == null )
        {
            return arguments;
        }

        object?[]? materialized = null;

        for ( var i = 0; i < arguments.Length; i++ )
        {
            if ( !IsCompilationBound( arguments[i] ) )
            {
                continue;
            }

            materialized ??= (object?[]) arguments.Clone();
            materialized[i] = formatter.Format( arguments[i] );
        }

        return materialized ?? arguments;
    }

    /// <summary>
    /// Determines whether an argument may reach a compilation, and may therefore not be stored in a diagnostic.
    /// </summary>
    /// <remarks>
    /// The default is <c>true</c>, so that a type nobody considered is materialized rather than retained. The cost of
    /// being wrong in that direction is that a message is formatted earlier than it needed to be; the cost of being
    /// wrong in the other direction is a retained compilation, which is what this method exists to prevent. Each
    /// <c>false</c> below therefore names a category that is either a value or a string, or whose formatting depends
    /// on the format specifier and must stay lazy.
    /// </remarks>
    private static bool IsCompilationBound( object? argument )
        => argument switch
        {
            null => false,
            string => false,

            // Formatted by ToDisplayString and ToDebugString respectively, neither of which reads the format
            // specifier. These are the categories that reach a compilation, and IDeclaration is among them.
            IDisplayable => true,
            ISymbol => true,
            IRef => true,

            // The two primitive types that do not implement IFormattable, and which the last case below would
            // otherwise materialize. Materializing them would produce the same message, since neither reads the format
            // specifier, so this is a matter of not doing needless work rather than of correctness, and no test can
            // tell the two behaviours apart.
            bool => false,
            char => false,

            // Enumerations, including the ones the formatter gives a display name to, and reflection types, which are
            // either real types or the identifier-based compile-time mocks. None of them reaches a compilation.
            Enum => false,
            Type => false,

            // The formatter gives an array of strings a presentation of its own, which materializing the array as a
            // whole would lose, and an array of strings cannot reach a compilation anyway.
            string?[] => false,

            // Any other array is formatted element by element with an empty specifier, so materializing the array as a
            // whole is faithful. It is worth doing only when an element requires it.
            Array array => AnyElementIsCompilationBound( array ),

            // The described object of an eligibility justification holds the declaration it describes. It is matched
            // before IFormattable, which it extends, and through IDescribedObject<object> rather than through a
            // non-generic base, which the interface does not have; its type parameter is covariant, so every
            // described object of a declaration matches. Its ToString ignores the format specifier.
            IDescribedObject<object> => true,

            // A composite format string holds arguments of its own, and one of them may be a declaration. It is
            // matched before IFormattable, which it implements. Materializing it is faithful whatever specifier it
            // was given, because FormattableString.ToString ignores the specifier and formats its own arguments.
            FormattableString formattable => AnyArgumentIsCompilationBound( formattable ),

            // Numbers, dates and the like, whose format specifier must reach them. This case is deliberately after
            // IDisplayable and ISymbol, which the formatter also matches first, and after FormattableString.
            IFormattable => false,

            _ => true
        };

    private static bool AnyArgumentIsCompilationBound( FormattableString formattable )
        => formattable.GetArguments().Any( IsCompilationBound );

    /// <summary>
    /// Determines whether any element of <paramref name="array"/> is compilation-bound, without the delegate and the
    /// iterator that the equivalent query expression would allocate on a path taken for every diagnostic.
    /// </summary>
    private static bool AnyElementIsCompilationBound( Array array )
    {
        foreach ( var element in array )
        {
            if ( IsCompilationBound( element ) )
            {
                return true;
            }
        }

        return false;
    }
}