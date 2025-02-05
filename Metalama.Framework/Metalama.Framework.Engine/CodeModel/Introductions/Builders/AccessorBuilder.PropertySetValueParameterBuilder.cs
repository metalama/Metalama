// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    private sealed class PropertySetValueParameterBuilder : ParameterBuilderBase
    {
        public PropertySetValueParameterBuilder( AccessorBuilder accessor, int index ) : base( accessor, index ) { }

        public override IType Type
        {
            get => ((IHasType) this.Accessor._containingMember).Type;
            set => throw new NotSupportedException( "Cannot directly change accessor's parameter type." );
        }

        public override RefKind RefKind
        {
            get
                => this.Accessor._containingMember switch
                {
                    PropertyBuilder propertyBuilder => propertyBuilder.RefKind,
                    FieldBuilder => RefKind.None,
                    _ => throw new AssertionFailedException( $"Unexpected containing member: '{this.Accessor._containingMember}'." )
                };

            set => throw new NotSupportedException( "Cannot directly change accessor's parameter reference kind." );
        }

        public override string Name
        {
            get => "value";
            set => throw new NotSupportedException( "Cannot set the name of a value parameter." );
        }
    }
}