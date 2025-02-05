// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class MethodCollection : MethodBaseCollection<IMethod>, IMethodCollection
    {
        public MethodCollection( INamedType declaringType, IUpdatableCollection<IFullRef<IMethod>> sourceItems ) : base(
            declaringType,
            sourceItems ) { }

        public IEnumerable<IMethod> OfKind( MethodKind kind ) => this.Where( m => m.MethodKind == kind );

        public IEnumerable<IMethod> OfKind( OperatorKind kind ) => this.Where( m => m.OperatorKind == kind );
    }
}