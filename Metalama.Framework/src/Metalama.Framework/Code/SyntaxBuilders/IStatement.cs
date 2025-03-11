// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Represents a statement, which can be inserted into run-time code using the <see cref="meta.InsertStatement(Metalama.Framework.Code.SyntaxBuilders.IStatement)"/>.
    /// To create a statement, use <see cref="StatementFactory"/>, <see cref="StatementBuilder"/>, or <see cref="SwitchStatementBuilder"/>.
    /// method.
    /// </summary>
    [CompileTime]
    [InternalImplement]
    public interface IStatement;
}