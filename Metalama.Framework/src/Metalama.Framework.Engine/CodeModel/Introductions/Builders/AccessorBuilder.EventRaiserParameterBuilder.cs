// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    private sealed class EventRaiserParameterBuilder : ParameterBuilderBase
    {
        private readonly IParameter _underlyingParameter;

        public EventRaiserParameterBuilder( AccessorBuilder accessor, IParameter underlyingParameter ) : base( accessor, underlyingParameter.Index )
        {
            this._underlyingParameter = underlyingParameter;
        }

        public override IType Type
        {
            get => this._underlyingParameter.Type;
            set => throw new NotSupportedException( "Cannot directly change a raiser parameter type." );
        }

        public override RefKind RefKind
        {
            get => this._underlyingParameter.RefKind;
            set => throw new NotSupportedException( "Cannot directly change a raiser parameter reference kind." );
        }

        public override string Name
        {
            get => this._underlyingParameter.Name;
            set => throw new NotSupportedException( "Cannot directory change a raiser parameter name." );
        }
    }
}