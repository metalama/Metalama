// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal abstract class MethodBaseCollection<T> : MemberCollection<T>
        where T : class, IMethodBase
    {
        protected MethodBaseCollection( INamedType declaringType, IUpdatableCollection<IFullRef<T>> sourceItems )
            : base( declaringType, sourceItems ) { }
    }
}