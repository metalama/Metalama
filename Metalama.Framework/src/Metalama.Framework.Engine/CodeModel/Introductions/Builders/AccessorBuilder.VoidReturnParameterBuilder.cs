// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities;
using System;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    private sealed class VoidReturnParameterBuilder : ParameterBuilderBase
    {
        public VoidReturnParameterBuilder( AccessorBuilder accessor ) : base( accessor, -1 ) { }

        [Memo]
        public override IType Type
        {
            get => this.Compilation.Cache.SystemVoidType;
            set => throw new NotSupportedException( "Cannot directly change accessor's parameter type." );
        }

        public override RefKind RefKind
        {
            get => RefKind.None;
            set => throw new NotSupportedException( "Cannot directly change accessor's parameter reference kind." );
        }

        public override string Name
        {
            get => "<return>";
            set => throw new NotSupportedException( "Cannot set the name of a return parameter." );
        }
    }
}