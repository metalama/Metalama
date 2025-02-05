// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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