// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    private sealed class EventValueParameterBuilder : ParameterBuilderBase
    {
        public EventValueParameterBuilder( AccessorBuilder accessor ) : base( accessor, 0 ) { }

        public override IType Type
        {
            get => ((EventBuilder) this.Accessor._containingMember).Type;
            set => throw new NotSupportedException( "Cannot directly change accessor's value parameter type." );
        }

        public override RefKind RefKind
        {
            get => RefKind.None;
            set => throw new NotSupportedException( "Cannot directly change accessor's value parameter reference kind." );
        }

        public override string Name
        {
            get => "value";
            set => throw new NotSupportedException( "Cannot set the name of a value parameter." );
        }
    }
}