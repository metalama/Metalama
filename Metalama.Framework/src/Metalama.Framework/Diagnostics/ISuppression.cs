// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents an instance of a diagnostic suppression that can be applied to declarations.
/// </summary>
/// <remarks>
/// <para>
/// This interface represents a suppression instance, which may be a <see cref="SuppressionDefinition"/> directly,
/// or a filtered suppression created by <see cref="SuppressionDefinition.WithFilter"/>.
/// </para>
/// <para>
/// Suppressions are applied using <see cref="ScopedDiagnosticSink.Suppress(ISuppression)"/> or
/// <see cref="IDiagnosticSink.Suppress"/> to prevent specific diagnostics from being reported within a declaration scope.
/// </para>
/// </remarks>
/// <seealso cref="SuppressionDefinition"/>
/// <seealso cref="ISuppressibleDiagnostic"/>
/// <seealso cref="IDiagnosticSink"/>
/// <seealso cref="ScopedDiagnosticSink"/>
/// <seealso href="@diagnostics"/>
[CompileTime]
[InternalImplement]
public interface ISuppression
{
    /// <summary>
    /// Gets the <see cref="SuppressionDefinition"/> that defines which diagnostic ID to suppress.
    /// </summary>
    SuppressionDefinition Definition { get; }

    /// <summary>
    /// Gets an optional filter predicate that determines which diagnostics should be suppressed.
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, all diagnostics with the ID specified in <see cref="Definition"/> are suppressed.
    /// If not <c>null</c>, only diagnostics for which the predicate returns <c>true</c> are suppressed.
    /// Use <see cref="SuppressionDefinition.WithFilter"/> to create filtered suppressions.
    /// </remarks>
    Func<ISuppressibleDiagnostic, bool>? Filter { get; }
}