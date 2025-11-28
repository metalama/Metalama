// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// A sink that reports diagnostics and suppressions from aspect code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides the low-level API for reporting diagnostics and suppressing diagnostics.
    /// Most aspect code should use <see cref="ScopedDiagnosticSink"/> instead, which provides a simpler API.
    /// </para>
    /// <para>
    /// Diagnostics are defined as static fields in aspect classes using <see cref="DiagnosticDefinition"/> or
    /// <see cref="DiagnosticDefinition{T}"/>, then reported using the <see cref="Report"/> method.
    /// Similarly, suppressions are defined using <see cref="SuppressionDefinition"/> and applied using
    /// the <see cref="Suppress"/> method.
    /// </para>
    /// </remarks>
    /// <seealso cref="ScopedDiagnosticSink"/>
    /// <seealso cref="IDiagnostic"/>
    /// <seealso cref="ISuppression"/>
    /// <seealso cref="IAdviser"/>
    /// <seealso href="@diagnostics"/>
    [CompileTime]
    [PublicAPI]
    [InternalImplement]
    public interface IDiagnosticSink
    {
        /// <summary>
        /// Reports a diagnostic at a specific location.
        /// </summary>
        /// <param name="diagnostic">The diagnostic to report, typically created from a <see cref="DiagnosticDefinition"/>.</param>
        /// <param name="location">The location where the diagnostic should be reported, or <c>null</c> to use a default location.</param>
        /// <param name="source">The source reporting the diagnostic.</param>
        void Report( IDiagnostic diagnostic, IDiagnosticLocation? location, IDiagnosticSource source );

        /// <summary>
        /// Suppresses a diagnostic within a specific declaration scope.
        /// </summary>
        /// <param name="suppression">The suppression definition, which specifies the diagnostic ID to suppress.</param>
        /// <param name="scope">The declaration within which the diagnostic should be suppressed.</param>
        /// <param name="source">The source performing the suppression.</param>
        void Suppress( ISuppression suppression, IDeclaration scope, IDiagnosticSource source );
    }
}