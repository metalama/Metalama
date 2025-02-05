// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal abstract class NamedDeclarationBuilder : DeclarationBuilder, INamedDeclarationBuilderImpl
{
    public abstract string Name { get; set; }

    protected NamedDeclarationBuilder( AspectLayerInstance aspectLayerInstance ) : base( aspectLayerInstance ) { }
}