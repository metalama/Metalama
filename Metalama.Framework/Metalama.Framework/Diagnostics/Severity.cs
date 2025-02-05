// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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