// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.Engine.CodeModel.Source.Pseudo
{
    internal sealed class PseudoSetter : PseudoAccessor
    {
        private readonly Accessibility? _accessibility;

        public override Accessibility Accessibility => this._accessibility ?? this.DeclaringMember.Accessibility;

        public PseudoSetter( IHasAccessorsImpl property, Accessibility? accessibility ) : base( property, MethodKind.PropertySet )
        {
            this._accessibility = accessibility;
        }

        [Memo]
        public override IParameterList Parameters => new PseudoParameterList( new PseudoParameter( this, 0, this.DeclaringMember.Type, "value" ) );

        public override string Name => "set_" + this.DeclaringMember.Name;
    }
}