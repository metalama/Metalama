// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    private sealed class PropertyGetReturnParameterBuilder : ParameterBuilderBase
    {
        public PropertyGetReturnParameterBuilder( AccessorBuilder accessor ) : base( accessor, -1 ) { }

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
                    IndexerBuilder indexerBuilder => indexerBuilder.RefKind,
                    FieldBuilder fieldBuilder => fieldBuilder.RefKind,
                    _ => throw new AssertionFailedException( $"Unexpected containing member: '{this.Accessor._containingMember}'." )
                };

            set => throw new NotSupportedException( "Cannot directly change accessor's parameter reference kind." );
        }

        public override string Name
        {
            get => "<return>";
            set => throw new NotSupportedException( "Cannot set the name of a return parameter." );
        }
    }
}