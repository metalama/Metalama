// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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