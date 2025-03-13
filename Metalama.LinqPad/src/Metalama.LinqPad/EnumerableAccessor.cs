// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Exposes the number of items in a collection.
    /// </summary>
    internal sealed class EnumerableAccessor
    {
        private readonly MethodInfo? _getter;
        private static readonly ConcurrentDictionary<Type, EnumerableAccessor> _instances = new();

        public static EnumerableAccessor Get( Type type ) => _instances.GetOrAdd( type, t => new EnumerableAccessor( t ) );

        private EnumerableAccessor( Type type )
        {
            this._getter = type.GetProperty( "Count", BindingFlags.Instance | BindingFlags.Public )?.GetMethod
                           ?? type.GetProperty( "Length", BindingFlags.Instance | BindingFlags.Public )?.GetMethod;

            // TODO: use a compiled Lambda expression.

            if ( this._getter != null && this._getter.ReturnType != typeof(int) )
            {
                this._getter = null;
            }
        }

        public bool HasCount => this._getter != null;

        public int GetCount( object obj ) => (int) this._getter.AssertNotNull().Invoke( obj, null )!;
    }
}