// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal abstract class IntroducedNamedDeclaration : IntroducedDeclaration, INamedDeclaration
{
    protected IntroducedNamedDeclaration( CompilationModel compilation, IGenericContext genericContext ) : base( compilation, genericContext ) { }

    protected abstract NamedDeclarationBuilderData NamedDeclarationBuilderData { get; }

    public string Name => this.NamedDeclarationBuilderData.Name;
}