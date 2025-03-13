// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Represents a list of statements. This list cannot be enumerated because it is evaluated late, when the statement is used in the target syntax tree.
/// To create an <see cref="IStatementList"/>, use <see cref="StatementFactory.List(Metalama.Framework.Code.SyntaxBuilders.IStatement[])"/>,
///  <see cref="StatementFactory.UnwrapBlock"/>, or <see cref="StatementExtensions.AsList(Metalama.Framework.Code.SyntaxBuilders.IStatement)"/>.
/// </summary>
public interface IStatementList;