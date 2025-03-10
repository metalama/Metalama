// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class FieldAndPropertiesCollection : IFieldOrPropertyCollection
    {
        private readonly IFieldCollection _fields;
        private readonly IPropertyCollection _properties;

        public FieldAndPropertiesCollection( IFieldCollection fields, IPropertyCollection properties )
        {
            this._fields = fields;
            this._properties = properties;
        }

        public INamedType DeclaringType => this._fields.DeclaringType;

        public IEnumerable<IFieldOrProperty> OfName( string name ) => this._fields.OfName( name ).Concat<IFieldOrProperty>( this._properties.OfName( name ) );

        public IEnumerator<IFieldOrProperty> GetEnumerator() => this._fields.Concat<IFieldOrProperty>( this._properties ).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._fields.Count + this._properties.Count;
    }
}