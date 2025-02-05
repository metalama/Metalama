// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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