// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Extracts the <see cref="ReflectionTypeLoadException.LoaderExceptions"/> of any <see cref="ReflectionTypeLoadException"/>
/// found in an exception tree.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Exception.ToString"/> does not render <see cref="ReflectionTypeLoadException.LoaderExceptions"/>,
/// so a report built from <see cref="Exception.ToString"/> alone says only that some types could not be loaded, never which
/// assembly failed to bind. The loader exceptions carry that information (typically a <see cref="System.IO.FileNotFoundException"/>
/// naming the unresolved assembly), which is the only actionable part of the report.
/// </para>
/// <para>
/// A single <see cref="ReflectionTypeLoadException"/> normally carries one loader exception per failing type, so the same
/// underlying binding failure is repeated many times. The results are therefore deduplicated by type and message.
/// </para>
/// </remarks>
[PublicAPI]
public static class LoaderExceptionsHelper
{
    /// <summary>
    /// The maximum number of distinct loader exceptions reported. A tree can legitimately contain thousands of them, and
    /// beyond a handful they no longer add information.
    /// </summary>
    private const int _maxReportedExceptions = 32;

    /// <summary>
    /// Gets the distinct <see cref="ReflectionTypeLoadException.LoaderExceptions"/> of a single
    /// <see cref="ReflectionTypeLoadException"/>, without walking its inner exceptions.
    /// </summary>
    /// <remarks>
    /// Use this overload from a renderer that already walks the exception tree itself, so that the loader exceptions of a
    /// nested <see cref="ReflectionTypeLoadException"/> are attributed to the exception that actually carries them.
    /// </remarks>
    public static ImmutableArray<Exception> GetDirectLoaderExceptions( ReflectionTypeLoadException exception )
    {
        var builder = ImmutableArray.CreateBuilder<Exception>();
        AddLoaderExceptions( exception, builder, new HashSet<string>( StringComparer.Ordinal ) );

        return builder.ToImmutable();
    }

    /// <summary>
    /// Gets the distinct loader exceptions of every <see cref="ReflectionTypeLoadException"/> in the tree of
    /// <paramref name="exception"/>, including its inner exceptions and, for an <see cref="AggregateException"/>, its
    /// inner exceptions. Returns an empty array when the tree contains no <see cref="ReflectionTypeLoadException"/>.
    /// </summary>
    public static ImmutableArray<Exception> GetLoaderExceptions( Exception? exception )
    {
        var builder = ImmutableArray.CreateBuilder<Exception>();
        var distinctKeys = new HashSet<string>( StringComparer.Ordinal );
        // Exception does not override Equals, so the default comparer is reference equality.
        var visited = new HashSet<Exception>();

        Visit( exception );

        return builder.ToImmutable();

        void Visit( Exception? e )
        {
            // The tree can contain the same instance twice (e.g. an AggregateException wrapping an exception that is also
            // the InnerException), so guard against visiting it repeatedly.
            if ( e == null || builder.Count >= _maxReportedExceptions || !visited.Add( e ) )
            {
                return;
            }

            if ( e is ReflectionTypeLoadException reflectionTypeLoadException )
            {
                AddLoaderExceptions( reflectionTypeLoadException, builder, distinctKeys );

                if ( builder.Count >= _maxReportedExceptions )
                {
                    return;
                }
            }

            Visit( e.InnerException );

            if ( e is AggregateException aggregateException )
            {
                foreach ( var innerException in aggregateException.InnerExceptions )
                {
                    Visit( innerException );
                }
            }
        }
    }

    /// <summary>
    /// Adds the loader exceptions of <paramref name="exception"/> to <paramref name="builder"/>, skipping null elements
    /// and those whose type and message are already represented in <paramref name="distinctKeys"/>.
    /// </summary>
    private static void AddLoaderExceptions( ReflectionTypeLoadException exception, ImmutableArray<Exception>.Builder builder, HashSet<string> distinctKeys )
    {
        // The declared element type is not nullable on all target frameworks, but the elements can be null.
        foreach ( var loaderException in exception.LoaderExceptions )
        {
            if ( builder.Count >= _maxReportedExceptions )
            {
                break;
            }

            if ( loaderException == null )
            {
                continue;
            }

            if ( distinctKeys.Add( $"{loaderException.GetType().FullName}: {loaderException.Message}" ) )
            {
                builder.Add( loaderException );
            }
        }
    }

    /// <summary>
    /// Renders the result of <see cref="GetLoaderExceptions"/> as text suitable for a crash report, or returns <c>null</c>
    /// when there is nothing to report.
    /// </summary>
    /// <param name="exception">The exception whose tree is searched.</param>
    /// <param name="scrub">An optional function removing sensitive data from the rendered messages and stack traces.</param>
    public static string? GetLoaderExceptionsText( Exception? exception, Func<string?, string>? scrub = null )
    {
        var loaderExceptions = GetLoaderExceptions( exception );

        if ( loaderExceptions.IsEmpty )
        {
            return null;
        }

        scrub ??= s => s ?? "";

        var stringBuilder = new StringBuilder();

        for ( var i = 0; i < loaderExceptions.Length; i++ )
        {
            var loaderException = loaderExceptions[i];

            stringBuilder.AppendLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0}] {1}: {2}",
                    i + 1,
                    loaderException.GetType().FullName,
                    scrub( loaderException.Message ) ) );

            if ( !string.IsNullOrEmpty( loaderException.StackTrace ) )
            {
                stringBuilder.AppendLine( scrub( loaderException.StackTrace ) );
            }
        }

        if ( loaderExceptions.Length >= _maxReportedExceptions )
        {
            stringBuilder.AppendLine(
                string.Format( CultureInfo.InvariantCulture, "(truncated after {0} distinct loader exceptions)", _maxReportedExceptions ) );
        }

        return stringBuilder.ToString();
    }
}
