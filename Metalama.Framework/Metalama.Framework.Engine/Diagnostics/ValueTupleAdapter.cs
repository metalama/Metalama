// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Metalama.Framework.Engine.Diagnostics
{
    /// <summary>
    /// Provides a method <see cref="ToArray"/> that converts a tuple into an array.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ValueTupleAdapter
    {
        private static readonly ConcurrentDictionary<Type, Func<object, object[]>> _cache = new();

        public static object[] ToArray( object valueTuple ) => _cache.GetOrAdd( valueTuple.GetType(), CreateAdapter ).Invoke( valueTuple );

        private static Func<object, object[]> CreateAdapter( Type type )
        {
            var arity = GetTupleSize( type );
            var input = Expression.Parameter( typeof(object) );
            var convertedInput = Expression.Convert( input, type );
            var fields = new Expression[arity];

            for ( var i = 0; i < arity; i++ )
            {
                fields[i] = GetItemExpression( convertedInput, i );
            }

            var array = Expression.NewArrayInit( typeof(object), fields );

            return Expression.Lambda<Func<object, object[]>>( array, input ).Compile();

            UnaryExpression GetItemExpression( Expression item, int index )
            {
                if ( index <= 6 )
                {
                    return Expression.Convert( Expression.Field( item, $"Item{index + 1}" ), typeof(object) );
                }
                else
                {
                    var rest = item;
                    var currentTupleType = type;
                    var depth = index / 7;

                    for ( var i = 0; i < depth; i++ )
                    {
                        var restType = currentTupleType.GetGenericArguments()[^1];
                        
                        rest = Expression.Convert( Expression.Field( rest, $"Rest" ), restType );

                        currentTupleType = restType;
                    }
                    
                    return Expression.Convert( Expression.Field( rest, $"Item{(index % 7) + 1}" ), typeof(object) );
                }
            }

            static int GetTupleSize( Type type )
            {
                var genericArguments = type.GetGenericArguments();

                if ( genericArguments.Length <= 7 )
                {
                    return genericArguments.Length;
                }
                else
                {
                    return 7 + GetTupleSize( genericArguments[^1] );
                }
            }
        }
    }
}