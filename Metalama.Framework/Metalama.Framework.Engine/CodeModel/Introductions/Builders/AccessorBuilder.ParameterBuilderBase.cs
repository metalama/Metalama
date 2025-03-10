// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    internal abstract class ParameterBuilderBase : BaseParameterBuilder
    {
        protected AccessorBuilder Accessor { get; }

        protected ParameterBuilderBase( AccessorBuilder accessor, int index ) : base( accessor.Compilation, accessor.AspectLayerInstance )
        {
            this.Accessor = accessor;
            this.Index = index;
        }

        public override TypedConstant? DefaultValue
        {
            get => null;
            set => throw new NotSupportedException( "Cannot set the default value of accessor parameter." );
        }

        public override RefKind RefKind { get; set; }

        public override int Index { get; }

        public sealed override bool IsParams
        {
            get => false;
            set => throw new NotSupportedException( "Cannot set the 'params' modifier on accessor parameter." );
        }

        public sealed override bool IsThis
        {
            get => false;
            set => throw new NotSupportedException( "Cannot set the 'this' modifier on accessor parameter." );
        }

        public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

        public override IHasParameters DeclaringMember => this.Accessor;

        public override ParameterInfo ToParameterInfo()
        {
            throw new NotImplementedException();
        }

        public override bool IsReturnParameter => this.Index < 0;

        public override bool CanBeInherited => ((IDeclarationImpl) this.DeclaringMember).CanBeInherited;

        public override bool IsDesignTimeObservable => this.Accessor.IsDesignTimeObservable;
    }
}