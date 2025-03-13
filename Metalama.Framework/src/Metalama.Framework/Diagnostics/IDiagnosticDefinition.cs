// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// A non-generic base interface for <see cref="DiagnosticDefinition{T}"/>.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    [CompileTime]
    [InternalImplement]
    public interface IDiagnosticDefinition
    {
        /// <summary>
        /// Gets the severity of the diagnostic.
        /// </summary>
        Severity Severity { get; }

        /// <summary>
        /// Gets an unique identifier for the diagnostic (e.g. <c>MY001</c>).
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the formatting string of the diagnostic message.
        /// </summary>
        string MessageFormat { get; }

        /// <summary>
        /// Gets the category of the diagnostic (e.g. your product name).
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Gets a short title describing the diagnostic. This title is typically described in the solution explorer of the IDE
        /// and does not contain formatting string parameters.
        /// </summary>
        string Title { get; }
    }
}