// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Represents a list of statements that can be used in contexts requiring multiple statements, such as switch case sections or block contents.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IStatementList"/> is a compile-time abstraction over a sequence of statements. Unlike regular collections, statement lists
/// cannot be enumerated at compile time because they are evaluated lazily when the statements are inserted into the target syntax tree.
/// </para>
/// <para>
/// To create an <see cref="IStatementList"/>, use one of the following methods:
/// <list type="bullet">
/// <item><see cref="StatementFactory.List(IStatement[])"/> - Create a list from a fixed array of statements</item>
/// <item><see cref="StatementListBuilder"/> - Build a list dynamically when the statements are determined at compile time</item>
/// <item><see cref="StatementFactory.UnwrapBlock"/> - Extract the statement list from a block statement</item>
/// <item><see cref="StatementExtensions.AsList(IStatement)"/> - Convert a single statement into a singleton list</item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IStatement"/>
/// <seealso cref="StatementFactory"/>
/// <seealso cref="StatementListBuilder"/>
/// <seealso href="@run-time-statements"/>
/// <seealso href="@templates"/>
public interface IStatementList;