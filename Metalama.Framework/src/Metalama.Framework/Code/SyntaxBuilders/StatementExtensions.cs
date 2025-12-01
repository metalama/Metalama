// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Extension methods for the <see cref="IStatement"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide convenient ways to convert statements to statement lists and to unwrap block statements.
/// Use <see cref="AsList(IStatement)"/> when you need to pass a single statement to an API that expects an <see cref="IStatementList"/>,
/// and <see cref="UnwrapBlock"/> when you need to extract the contents of a block statement.
/// </para>
/// </remarks>
/// <seealso cref="IStatement"/>
/// <seealso cref="IStatementList"/>
/// <seealso cref="StatementFactory"/>
/// <seealso cref="StatementListBuilder"/>
/// <seealso href="@run-time-statements"/>
/// <seealso href="@templates"/>
[CompileTime]
public static class StatementExtensions
{
    /// <summary>
    /// Wraps a given <see cref="IStatement"/> into a singleton <see cref="IStatementList"/>.
    /// </summary>
    /// <param name="statement">The statement to wrap.</param>
    /// <returns>A statement list containing the single statement.</returns>
    public static IStatementList AsList( this IStatement statement ) => StatementFactory.List( statement );

    /// <summary>
    /// Wraps a list of <see cref="IStatement"/> into an <see cref="IStatementList"/>.
    /// </summary>
    /// <param name="statements">The statements to wrap.</param>
    /// <returns>A statement list containing all the statements.</returns>
    public static IStatementList AsList( this IEnumerable<IStatement> statements ) => StatementFactory.List( statements );

    /// <summary>
    /// Unwraps a block (i.e. remove its braces), if the statement is a block, and returns the resulting <see cref="IStatementList"/>.
    /// </summary>
    /// <param name="statement">The statement to unwrap.</param>
    /// <returns>A statement list containing the statements within the block, or a list with the original statement if it is not a block.</returns>
    public static IStatementList UnwrapBlock( this IStatement statement ) => StatementFactory.UnwrapBlock( statement );
}