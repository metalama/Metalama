// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime.Manifest;

internal sealed class TemplateProjectManifestBuilder
{
    private readonly TemplateSymbolManifest.Builder _rootSymbolBuilder;

    public TemplateProjectManifestBuilder( Compilation compilation ) : this( compilation.SourceModule.GlobalNamespace ) { }

    public TemplateProjectManifestBuilder( INamespaceSymbol ns )
    {
        this._rootSymbolBuilder = new TemplateSymbolManifest.Builder( ns );
    }

    public void AddOrUpdateSymbol( ISymbol symbol, TemplatingScope? scope = null, TemplateInfo? templateInfo = null, RoslynApiVersion? usedApiVersion = null )
        => this._rootSymbolBuilder.AddOrUpdateSymbol( symbol, scope, templateInfo, usedApiVersion );

    public TemplateProjectManifest Build()
    {
        return new TemplateProjectManifest( this._rootSymbolBuilder.Build() );
    }
}