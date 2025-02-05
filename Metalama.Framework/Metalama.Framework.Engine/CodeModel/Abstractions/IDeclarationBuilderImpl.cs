// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;

namespace Metalama.Framework.Engine.CodeModel.Abstractions
{
    internal interface IDeclarationBuilderImpl : IDeclarationBuilder, IDeclarationImpl
    {
        AspectLayerInstance AspectLayerInstance { get; }

        new AttributeBuilderCollection Attributes { get; }

        bool IsDesignTimeObservable { get; }
    }
}