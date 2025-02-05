// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Code.Collections;

internal sealed class NamedArgumentList : INamedArgumentList
{
    private readonly IReadOnlyList<KeyValuePair<string, TypedConstant>> _arguments;

    public static NamedArgumentList Empty { get; } = new( null );

    internal NamedArgumentList( IReadOnlyList<KeyValuePair<string, TypedConstant>>? arguments )
    {
        this._arguments = arguments ?? ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;
    }

    public IEnumerator<KeyValuePair<string, TypedConstant>> GetEnumerator() => this._arguments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => this._arguments.Count;

    public KeyValuePair<string, TypedConstant> this[ int index ] => this._arguments[index];

    public bool ContainsKey( string key ) => this._arguments.Any( a => string.Equals( a.Key, key, StringComparison.Ordinal ) );

    public bool TryGetValue( string key, out TypedConstant value )
    {
        var argument = this._arguments.SingleOrDefault( a => a.Key == key );

        if ( argument.Key != null! )
        {
            value = argument.Value;

            return true;
        }
        else
        {
            value = default;

            return false;
        }
    }

    public TypedConstant this[ string key ] => this._arguments.Single( a => string.Equals( a.Key, key, StringComparison.Ordinal ) ).Value;

    public IEnumerable<string> Keys => this._arguments.Select( a => a.Key );

    public IEnumerable<TypedConstant> Values => this._arguments.Select( a => a.Value );
}