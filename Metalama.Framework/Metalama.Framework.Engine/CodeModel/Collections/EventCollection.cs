// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class EventCollection : MemberCollection<IEvent>, IEventCollection
    {
        public EventCollection( INamedType declaringType, EventUpdatableCollection sourceItems ) : base( declaringType, sourceItems ) { }
    }
}