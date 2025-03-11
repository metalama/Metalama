// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Introspection
{
    /// <summary>
    /// Represents a diagnostic (error, warning, information, hidden message).
    /// </summary>
    [PublicAPI]
    public interface IIntrospectionDiagnostic
    {
        /// <summary>
        /// Gets the compilation in which the diagnostic was reported. 
        /// </summary>
        ICompilation? Compilation { get; }

        /// <summary>
        /// Gets the diagnostic id (e.g. <c>CS0123</c>).
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the formatted diagnostic message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the path of the file in which the diagnostic was reported.
        /// </summary>
        string? FilePath { get; }

        /// <summary>
        /// Gets the line at which the diagnostic was reported.
        /// </summary>
        int? Line { get; }

        /// <summary>
        /// Gets the declaration in which the diagnostic was reported.
        /// </summary>
        IDeclaration? Declaration { get; }

        /// <summary>
        /// Gets the diagnostic severity.
        /// </summary>
        Severity Severity { get; }

        /// <summary>
        /// Gets the source (<see cref="IntrospectionDiagnosticSource.Metalama"/> or <see cref="IntrospectionDiagnosticSource.CSharp"/>)
        /// of the diagnostic.
        /// </summary>
        IntrospectionDiagnosticSource Source { get; }

        object? Details { get; }
    }
}