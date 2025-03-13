// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In PostSharp, this class allowed the run-time code of the aspect to access a property in the target code. In Metalama,
    /// no run-time helper is required because the template directly generates run-time code.
    /// Use <see cref="Metalama.Framework.Code.IProperty"/>.<see cref="IExpression.Value"/> to generate run-time code for any property.
    /// </summary>
    [PublicAPI]
    public sealed class Property<TValue> : IProperty
    {
        public Property( PropertyGetter<TValue> getter, PropertySetter<TValue> setter )
        {
            throw new NotImplementedException();
        }

        public PropertyGetter<TValue> Get;

        public PropertySetter<TValue> Set;

        object IProperty.GetValue()
        {
            throw new NotImplementedException();
        }

        void IProperty.SetValue( object value )
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// In PostSharp, this class allowed the run-time code of the aspect to access an indexer in the target code. In Metalama,
    /// no run-time helper is required because the template directly generates run-time code.
    /// Use <see cref="IIndexer"/>.<see cref="IIndexerInvoker.GetValue"/> and <see cref="IIndexer"/>.<see cref="IIndexerInvoker.SetValue"/>
    /// to generate run-time code for any indexer.
    /// </summary>
    [PublicAPI]
    public sealed class Property<TValue, TIndex>
    {
        public Property( PropertyGetter<TValue, TIndex> getter, PropertySetter<TValue, TIndex> setter )
        {
            throw new NotImplementedException();
        }

        public PropertyGetter<TValue, TIndex> Get;

        public PropertySetter<TValue, TIndex> Set;
    }

    /// <summary>
    /// In PostSharp, this delegate allowed the run-time code of the aspect to access an indexer in the target code. In Metalama,
    /// no run-time helper is required because the template directly generates run-time code.
    /// Use <see cref="IIndexer"/>.<see cref="IIndexerInvoker.GetValue"/> and <see cref="IIndexer"/>.<see cref="IIndexerInvoker.SetValue"/>
    /// to generate run-time code for any indexer.
    /// </summary>
    public delegate TValue PropertyGetter<TValue, TIndex>( TIndex index );

    /// <summary>
    /// In PostSharp, this delegate allowed the run-time code of the aspect to access an indexer in the target code. In Metalama,
    /// no run-time helper is required because the template directly generates run-time code.
    /// Use <see cref="IIndexer"/>.<see cref="IIndexerInvoker.GetValue"/> and <see cref="IIndexer"/>.<see cref="IIndexerInvoker.SetValue"/>
    /// to generate run-time code for any indexer.
    /// </summary>
    public delegate void PropertySetter<TValue, TIndex>( TIndex index, TValue value );
}