// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Allows building lists of statements programmatically by adding <see cref="IStatement"/> objects one at a time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StatementListBuilder"/> provides a way to dynamically construct a list of statements when the number or
/// content of statements is determined at compile time. Use <see cref="Add(IStatement)"/> to add individual statements
/// or <see cref="Add(IStatementList)"/> to add entire statement lists. After adding all statements, call
/// <see cref="ToStatementList"/> to get an <see cref="IStatementList"/> that can be used with other builders like
/// <see cref="SwitchStatementBuilder"/> or with <see cref="StatementFactory.Block(IStatementList)"/>.
/// </para>
/// </remarks>
/// <seealso cref="IStatementList"/>
/// <seealso cref="StatementFactory"/>
/// <seealso cref="SwitchStatementBuilder"/>
/// <seealso href="@run-time-statements"/>
/// <seealso href="@templates"/>
[CompileTime]
[PublicAPI]
public class StatementListBuilder
{
    private readonly List<object> _items = new();

    /// <summary>
    /// Appends an <see cref="IStatement"/> to the list.
    /// </summary>
    public void Add( IStatement statement ) => this._items.Add( statement );

    /// <summary>
    /// Appends an <see cref="IStatementList"/> to the list.
    /// </summary>
    /// <param name="statementList"></param>
    public void Add( IStatementList statementList ) => this._items.Add( statementList );

    /// <summary>
    /// Creates an <see cref="IStatementList"/> from the current object.
    /// </summary>
    public IStatementList ToStatementList() => SyntaxBuilder.CurrentImplementation.CreateStatementList( this._items.ToImmutableArray() );
}