// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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