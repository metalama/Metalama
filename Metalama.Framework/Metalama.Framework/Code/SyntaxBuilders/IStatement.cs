// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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