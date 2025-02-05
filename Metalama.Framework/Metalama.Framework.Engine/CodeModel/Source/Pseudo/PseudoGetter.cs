// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;

namespace Metalama.Framework.Engine.CodeModel.Source.Pseudo
{
    internal sealed class PseudoGetter : PseudoAccessor
    {
        public override Accessibility Accessibility => this.DeclaringMember.Accessibility;

        public PseudoGetter( IHasAccessorsImpl property ) : base( property, MethodKind.PropertyGet ) { }

        public override IParameterList Parameters => ParameterList.Empty;

        public override string Name => "get_" + this.DeclaringMember.Name;
    }
}