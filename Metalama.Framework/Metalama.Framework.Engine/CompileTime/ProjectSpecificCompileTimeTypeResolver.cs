// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class ProjectSpecificCompileTimeTypeResolver : CompileTimeTypeResolver
{
    private readonly CompileTimeTypeResolver _systemTypeResolver;
    private readonly CompileTimeProjectRepository _projectRepository;

    private ProjectSpecificCompileTimeTypeResolver( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext ) :
        base( compilationContext )
    {
        this._projectRepository = serviceProvider.GetRequiredService<CompileTimeProjectRepository>();
        this._systemTypeResolver = serviceProvider.GetRequiredService<SystemTypeResolver.Provider>().Get( compilationContext );
    }

    /// <summary>
    /// Gets a compile-time reflection <see cref="Type"/> given its Roslyn symbol.
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override Type? GetCompileTimeNamedType( INamedTypeSymbol typeSymbol, CancellationToken cancellationToken = default )
    {
        // Check if the type is a .NET system one.
        var systemType = this._systemTypeResolver.GetCompileTimeType( typeSymbol, false, cancellationToken );

        if ( systemType != null )
        {
            return systemType;
        }

        // The type is not a system one. Check if it is a compile-time one.
        return this.Cache.GetOrAdd( typeSymbol, this.GetCompileTimeNamedTypeCore );
    }

    private Type? GetCompileTimeNamedTypeCore( ITypeSymbol typeSymbol )
    {
        var assemblySymbol = typeSymbol.ContainingAssembly;

        if ( !this._projectRepository.TryGetCompileTimeProject( assemblySymbol.Identity, out var compileTimeProject ) )
        {
            return null;
        }

        var reflectionName = typeSymbol.GetReflectionFullName();

        return compileTimeProject?.GetTypeOrNull( reflectionName );
    }

    public sealed class Provider : CompilationServiceProvider<ProjectSpecificCompileTimeTypeResolver>
    {
        public Provider( in ProjectServiceProvider serviceProvider ) : base( in serviceProvider ) { }

        protected override ProjectSpecificCompileTimeTypeResolver Create( CompilationContext compilationContext )
            => new( this.ServiceProvider, compilationContext );
    }
}