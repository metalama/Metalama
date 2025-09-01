// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Utilities;
using System;
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
                new[] {
                    new PseudoParameter(this, 0, ((IEvent)this.ContainingDeclaration).Type, "handler")
                }.Concat(
                    ((INamedType) this.DeclaringMember.Type).Methods.OfName( "Invoke" )
                    .Single()
                    .Parameters.SelectAsImmutableArray( p => new PseudoParameter( this, p.Index + 1, p.Type, p.Name, "args" ) ) ) );

        public override string Name => "raise_" + this.DeclaringMember.Name;
    }
}