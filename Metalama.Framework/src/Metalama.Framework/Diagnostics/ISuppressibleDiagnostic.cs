// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents a compiler or analyzer diagnostic that can be selectively suppressed using filtered suppressions.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides access to diagnostic properties that can be used in suppression filters created with
/// <see cref="SuppressionDefinition.WithFilter"/>. When a suppression has a filter, each diagnostic matching
/// the suppression ID is passed to the filter predicate as an <see cref="ISuppressibleDiagnostic"/>, allowing
/// the filter to inspect the diagnostic's message, arguments, or location to decide whether to suppress it.
/// </para>
/// </remarks>
/// <seealso cref="ISuppression"/>
/// <seealso cref="SuppressionDefinition"/>
/// <seealso cref="SuppressionDefinition.WithFilter"/>
/// <seealso cref="IDiagnostic"/>
/// <seealso href="@diagnostics"/>
[InternalImplement]
[CompileTime]
public interface ISuppressibleDiagnostic
{
    /// <summary>
    /// Gets the diagnostic ID (e.g., <c>CS0169</c> for "field is never used").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the diagnostic message formatted in the invariant (English) culture.
    /// </summary>
    /// <remarks>
    /// Use this property in suppression filters to match diagnostics by message content.
    /// </remarks>
    string InvariantMessage { get; }

    /// <summary>
    /// Gets the arguments passed to the diagnostic message formatter.
    /// </summary>
    /// <remarks>
    /// These arguments often include symbols like members or types related to the diagnostic.
    /// Use this property in suppression filters to match diagnostics based on specific symbols or values.
    /// </remarks>
    ImmutableArray<object?> Arguments { get; }

    /// <summary>
    /// Gets the source location (file, line, and column) of the diagnostic, when available.
    /// </summary>
    SourceSpan? Span { get; }
}