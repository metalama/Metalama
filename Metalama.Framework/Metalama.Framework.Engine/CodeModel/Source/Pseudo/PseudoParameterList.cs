// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Invokers;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source.Pseudo
{
    internal sealed class PseudoParameterList : List<PseudoParameter>, IParameterList
    {
        public PseudoParameterList( PseudoParameter parameter ) : base( 1 )
        {
            this.Add( parameter );
        }

        public PseudoParameterList( IEnumerable<PseudoParameter> collection ) : base( collection ) { }

        IEnumerator<IParameter> IEnumerable<IParameter>.GetEnumerator() => this.GetEnumerator();

        IParameter IReadOnlyList<IParameter>.this[ int index ] => this[index];

        public IParameter this[ string name ] => this.Single<IParameter>( p => p.Name == name );

        public object ToValueArray() => new ValueArrayExpression( this );
    }
}