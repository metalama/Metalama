// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// A compile-time representation of a run-time statement that can be inserted into generated code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Metalama templates, <see cref="IStatement"/> objects are compile-time representations of C# statements that will be
    /// inserted into the transformed code. Unlike <see cref="IExpression"/> which represents values and can be used within
    /// expressions, statements represent complete actions that are inserted as separate lines of code using
    /// <see cref="meta.InsertStatement(IStatement)"/>.
    /// </para>
    /// <para>
    /// To create statements, use <see cref="StatementFactory"/> for common scenarios (parsing strings, creating blocks, invoking
    /// templates), <see cref="StatementBuilder"/> for programmatic construction with proper indentation support, or
    /// <see cref="SwitchStatementBuilder"/> for building switch statements.
    /// </para>
    /// </remarks>
    /// <seealso cref="StatementFactory"/>
    /// <seealso cref="StatementBuilder"/>
    /// <seealso cref="SwitchStatementBuilder"/>
    /// <seealso cref="IExpression"/>
    /// <seealso href="@run-time-statements"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    [InternalImplement]
    public interface IStatement;
}