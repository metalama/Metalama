// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// Specifies the severity level of a diagnostic.
    /// </summary>
    /// <seealso cref="IDiagnosticDefinition"/>
    /// <seealso cref="DiagnosticDefinition"/>
    /// <seealso cref="DiagnosticDefinition{T}"/>
    /// <seealso href="@diagnostics"/>
    [CompileTime]
    public enum Severity
    {
        /// <summary>
        /// An issue that is not surfaced through normal IDE or compiler output but may be acted upon by other tools or mechanisms.
        /// </summary>
        /// <remarks>
        /// Use this for diagnostics that should not appear in the error list or output window but might be used
        /// by code analysis tools or other automated processes.
        /// </remarks>
        Hidden = 0,

        /// <summary>
        /// Informational message that does not indicate a problem.
        /// </summary>
        Info = 1,

        /// <summary>
        /// A potential issue that is allowed but may indicate a problem.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// A violation of aspect rules that prevents successful compilation.
        /// </summary>
        Error = 3
    }
}