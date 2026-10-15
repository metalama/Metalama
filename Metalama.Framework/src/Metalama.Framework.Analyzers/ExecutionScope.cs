// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Analyzers.Immutability
{
    /// <summary>
    /// The execution scope of a type, as far as this analyzer needs to know it.
    /// </summary>
    /// <remarks>
    /// This is a subset of the <c>TemplatingScope</c> enumeration of the engine, which this project cannot reference.
    /// See <see cref="ImmutabilityContext.GetExecutionScope"/> for the rules that compute it.
    /// </remarks>
    internal enum ExecutionScope
    {
        /// <summary>
        /// The type exists only in the run-time assembly.
        /// </summary>
        RunTime,

        /// <summary>
        /// The type exists both in the run-time assembly and in the compile-time assembly.
        /// </summary>
        RunTimeOrCompileTime,

        /// <summary>
        /// The type exists only in the compile-time assembly.
        /// </summary>
        CompileTime
    }
}
