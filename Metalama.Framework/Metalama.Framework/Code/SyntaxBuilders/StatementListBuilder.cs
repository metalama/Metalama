// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// A class that allows to dynamically build an <see cref="IStatementList"/>.
/// </summary>
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