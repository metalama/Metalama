// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static partial class LinqExtensions
{
    private class SelectList<TIn, TOut> : INonMaterialized, IReadOnlyList<TOut>
    {
        private readonly IReadOnlyList<TIn> _input;
        private readonly Func<TIn, TOut> _func;

#if DEBUG
        private bool _isAlreadyEvaluated;
#endif

        public SelectList( IReadOnlyList<TIn> input, Func<TIn, TOut> func )
        {
            this._input = input;
            this._func = func;
        }

        public IEnumerator<TOut> GetEnumerator()
        {
            for ( var i = 0; i < this.Count; i++ )
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._input.Count;

        public TOut this[ int index ]
        {
            get
            {
#if DEBUG

                // This is a heuristic to check that we don't evaluate the func several times
                // for the same item. In this case, the current class should not be used
                // and the query should be materialized.

                if ( index == 0 )
                {
                    if ( this._isAlreadyEvaluated )
                    {
                        throw new AssertionFailedException( "The SelectList was evaluated twice." );
                    }
                    else
                    {
                        this._isAlreadyEvaluated = true;
                    }
                }
#endif
                return this._func( this._input[index] );
            }
        }
    }
}