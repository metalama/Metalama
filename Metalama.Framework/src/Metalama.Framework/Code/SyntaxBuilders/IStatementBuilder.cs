// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// A common interface for objects that produce an <see cref="IStatement"/>.
/// </summary>
[CompileTime]
public interface IStatementBuilder
{
    /// <summary>
    /// Builds an <see cref="IStatement"/> representing the current object.
    /// </summary>
    IStatement ToStatement();
}