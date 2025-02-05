// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static partial class LinqExtensions
{
    private class SelectCollection<TIn, TOut> : INonMaterialized, IReadOnlyCollection<TOut>
    {
        private readonly IReadOnlyCollection<TIn> _input;
        private readonly Func<TIn, TOut> _func;

#if DEBUG
        private bool _isAlreadyEvaluated;
#endif

        public SelectCollection( IReadOnlyCollection<TIn> input, Func<TIn, TOut> func )
        {
            this._input = input;
            this._func = func;
        }

        public IEnumerator<TOut> GetEnumerator()
        {
#if DEBUG

            // This is a heuristic to check that we don't evaluate the func several times
            // for the same item. In this case, the current class should not be used
            // and the query should be materialized.

            if ( this._isAlreadyEvaluated )
            {
                throw new AssertionFailedException( "The SelectCollection was evaluated twice." );
            }
            else
            {
                this._isAlreadyEvaluated = true;
            }
#endif
            foreach ( var item in this._input )
            {
                yield return this._func( item );
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._input.Count;
    }
}