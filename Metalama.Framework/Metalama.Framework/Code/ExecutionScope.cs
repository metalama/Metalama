// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Enumerates the possible execution scopes of a declaration i.e. <see cref="RunTime"/>, <see cref="CompileTime"/> or <see cref="RunTimeOrCompileTime"/>.
/// </summary>
[CompileTime]
public enum ExecutionScope
{
    /// <summary>
    /// Equal to <see cref="RunTime"/>.
    /// </summary>
    Default,

    /// <summary>
    /// Run-time-only declaration.
    /// </summary>
    RunTime = Default,

    /// <summary>
    /// Compile-time-only declaration. Typically a type annotated with <see cref="CompileTimeAttribute"/>.
    /// </summary>
    CompileTime,

    /// <summary>
    /// Run-time-or-compile-time declaration. Typically an aspect or a type annotated with <see cref="RunTimeOrCompileTimeAttribute"/>.
    /// </summary>
    RunTimeOrCompileTime
}