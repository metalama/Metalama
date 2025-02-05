// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline.Diff
{
    /// <summary>
    /// Kinds of change of the compile-time status of a syntax tree.
    /// </summary>
    internal enum CompileTimeChangeKind
    {
        /// <summary>
        /// No change in the compile-time status.
        /// </summary>
        None,

        /// <summary>
        /// The syntax tree is newly compile-time.
        /// </summary>
        NewlyCompileTime,

        /// <summary>
        /// The syntax tree is no longer compile-time.
        /// </summary>
        NoLongerCompileTime
    }
}