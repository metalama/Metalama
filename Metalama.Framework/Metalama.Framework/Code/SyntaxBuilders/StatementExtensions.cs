// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Extensions to the <see cref="IStatement"/> interface.
/// </summary>
[CompileTime]
public static class StatementExtensions
{
    /// <summary>
    /// Wraps a given <see cref="IStatement"/> into a singleton <see cref="IStatementList"/>.
    /// </summary>
    public static IStatementList AsList( this IStatement statement ) => StatementFactory.List( statement );

    /// <summary>
    /// Wraps a list of <see cref="IStatement"/> into an <see cref="IStatementList"/>.
    /// </summary>
    public static IStatementList AsList( this IEnumerable<IStatement> statements ) => StatementFactory.List( statements );

    /// <summary>
    /// Unwraps a block (i.e. remove its braces), if the statement is a block, and returns the resulting <see cref="IStatementList"/>.
    /// </summary>
    public static IStatementList UnwrapBlock( this IStatement statement ) => StatementFactory.UnwrapBlock( statement );
}