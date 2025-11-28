// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// A diagnostic sink with a default scope that simplifies reporting diagnostics and suppressions.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a simplified API for reporting diagnostics and suppressions by maintaining
/// a default target location and declaration. This eliminates the need to specify the location or scope
/// with each call, unlike <see cref="IDiagnosticSink"/> which requires explicit locations and scopes.
/// </para>
/// <para>
/// Instances of <see cref="ScopedDiagnosticSink"/> are typically accessed through
/// <see cref="IAdviser.Diagnostics"/> in aspect code.
/// </para>
/// </remarks>
/// <seealso cref="ScopedDiagnosticSink"/>
/// <seealso cref="IDiagnosticSink"/>
/// <seealso cref="IDiagnostic"/>
/// <seealso cref="ISuppression"/>
/// <seealso href="@diagnostics"/>
public interface IScopedDiagnosticSink
{
    /// <summary>
    /// Gets the underlying diagnostic sink.
    /// </summary>
    IDiagnosticSink Sink { get; }

    /// <summary>
    /// Gets the source reporting diagnostics or suppressions.
    /// </summary>
    IDiagnosticSource Source { get; }

    /// <summary>
    /// Reports a diagnostic to the default location of this scoped sink.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void Report( IDiagnostic diagnostic );

    /// <summary>
    /// Suppresses a diagnostic within the default declaration scope of this scoped sink.
    /// </summary>
    /// <param name="suppression">The suppression definition specifying which diagnostic to suppress.</param>
    void Suppress( ISuppression suppression );
}