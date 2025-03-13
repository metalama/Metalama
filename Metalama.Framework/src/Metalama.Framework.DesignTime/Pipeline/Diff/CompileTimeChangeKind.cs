// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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