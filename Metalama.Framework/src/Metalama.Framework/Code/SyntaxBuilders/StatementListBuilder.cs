// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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