// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class PropertyCollection : MemberCollection<IProperty>, IPropertyCollection
    {
        public PropertyCollection( INamedType declaringType, PropertyUpdatableCollection sourceItems ) : base( declaringType, sourceItems ) { }

        public IProperty this[ string name ] => this.OfName( name ).Single();
    }
}