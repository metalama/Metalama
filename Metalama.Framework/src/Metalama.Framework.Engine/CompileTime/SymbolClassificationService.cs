// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class SymbolClassificationService : ITemplateInfoService
{
    private readonly CompileTimeProjectRepository _repository;

    internal SymbolClassificationService( CompileTimeProjectRepository repository )
    {
        this._repository = repository;
    }

    internal static SymbolClassificationService CreateTestInstance() => new( CompileTimeProjectRepository.CreateTestInstance() );

    private TemplateProjectManifest? GetManifest( IAssemblySymbol assembly )
    {
        this._repository.TryGetCompileTimeProject( assembly.Identity, out var project );

        return project?.Manifest?.Templates;
    }

    ITemplateInfo ITemplateInfoService.GetTemplateInfo( ISymbol symbol ) => this.GetTemplateInfo( symbol );

    private ITemplateInfo GetTemplateInfo( ISymbol symbol )
        => symbol.ContainingAssembly != null
            ? this.GetManifest( symbol.ContainingAssembly )?.GetTemplateInfo( symbol ) ?? NullTemplateInfo.Instance
            : NullTemplateInfo.Instance;

    public ExecutionScope GetExecutionScope( ISymbol symbol )
        => symbol.ContainingAssembly != null
            ? this.GetManifest( symbol.ContainingAssembly )?.GetExecutionScope( symbol ) ?? ExecutionScope.RunTime
            : ExecutionScope.RunTime;

    public bool IsTemplate( ISymbol symbol )
        => symbol.ContainingAssembly != null
           && (this.GetManifest( symbol.ContainingAssembly )?.IsTemplate( symbol ) ?? false);

    public bool IsCompileTimeParameter( IParameterSymbol symbol ) => this.GetExecutionScope( symbol ) == ExecutionScope.CompileTime;

    public bool IsCompileTimeTypeParameter( ITypeParameterSymbol symbol ) => this.GetExecutionScope( symbol ) == ExecutionScope.CompileTime;
}

internal interface ITemplateInfoService : ISymbolClassificationService
{
    ITemplateInfo GetTemplateInfo( ISymbol symbol );
}