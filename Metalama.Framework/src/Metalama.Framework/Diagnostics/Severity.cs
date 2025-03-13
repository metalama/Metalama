// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// Severity of diagnostics.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    [CompileTime]
    public enum Severity
    {
        /// <summary>
        /// Something that is an issue, but is not surfaced through normal means.
        /// There may be different mechanisms that act on these issues.
        /// </summary>
        Hidden = 0,

        /// <summary>
        /// Information that does not indicate a problem (i.e. not prescriptive).
        /// </summary>
        Info = 1,

        /// <summary>
        /// Something suspicious but allowed.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Something not allowed by the rules of the aspect.
        /// </summary>
        Error = 3
    }
}