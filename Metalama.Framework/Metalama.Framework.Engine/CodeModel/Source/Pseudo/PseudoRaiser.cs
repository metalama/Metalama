// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Utilities;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source.Pseudo
{
    internal sealed class PseudoRaiser : PseudoAccessor
    {
        public override Accessibility Accessibility => this.DeclaringMember.Accessibility;

        public PseudoRaiser( SourceEvent @event ) : base( @event, MethodKind.EventRaise ) { }

        [Memo]
        public override IParameterList Parameters
            => new PseudoParameterList(
                ((INamedType) this.DeclaringMember.Type).Methods.OfName( "Invoke" )
                .Single()
                .Parameters.SelectAsImmutableArray( p => new PseudoParameter( this, p.Index, p.Type, p.Name ) ) );

        public override string Name => "raise_" + this.DeclaringMember.Name;
    }
}