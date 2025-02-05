// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel;

internal sealed class DeclarationOrigin : IDeclarationOrigin
{
    public static IDeclarationOrigin Source { get; } = new DeclarationOrigin( DeclarationOriginKind.Source, false );

    public static IDeclarationOrigin CompilerGeneratedSource { get; } = new DeclarationOrigin( DeclarationOriginKind.Source, true );

    public static IDeclarationOrigin External { get; } = new DeclarationOrigin( DeclarationOriginKind.External, false );

    public static IDeclarationOrigin CompilerGeneratedExternal { get; } = new DeclarationOrigin( DeclarationOriginKind.External, false );

    private DeclarationOrigin( DeclarationOriginKind kind, bool isCompilerGenerated )
    {
        this.Kind = kind;
        this.IsCompilerGenerated = isCompilerGenerated;
    }

    public DeclarationOriginKind Kind { get; }

    public bool IsCompilerGenerated { get; }

    public override string ToString() => this.Kind.ToString();
}