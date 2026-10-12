// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class ProjectSpecificCompileTimeTypeResolver : CompileTimeTypeResolver, IProjectService
{
    private readonly CompileTimeTypeResolver _systemTypeResolver;
    private readonly CompileTimeProjectRepository _projectRepository;

    public ProjectSpecificCompileTimeTypeResolver( in ProjectServiceProvider serviceProvider )
        : base( serviceProvider.GetRequiredService<CompileTimeTypeFactory>() )
    {
        this._projectRepository = serviceProvider.GetRequiredService<CompileTimeProjectRepository>();
        this._systemTypeResolver = serviceProvider.GetRequiredService<SystemTypeResolver>();
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
        var reflectionName = typeSymbol.GetReflectionFullName();

        if ( !this._projectRepository.TryGetCompileTimeProject( assemblySymbol.Identity, out var compileTimeProject ) )
        {
            return this.GetTypeFromDomain( assemblySymbol.Identity, reflectionName );
        }

        return compileTimeProject?.GetTypeOrNull( reflectionName );
    }

    /// <summary>
    /// Gets a type of an assembly that has no compile-time project but is loaded into the <see cref="CompileTimeDomain"/>.
    /// </summary>
    /// <remarks>
    /// An assembly that is referenced with the <c>MetalamaCompileTimeAssembly</c> item is a standard assembly, so it has no compile-time project.
    /// When it is also an extension assembly, it is loaded only into the <see cref="CompileTimeDomain"/>, so the <see cref="SystemTypeResolver"/>
    /// cannot find its types. This is the case of the public assembly of a Metalama extension package, which can define a custom attribute
    /// that the pipeline instantiates, for instance an <see cref="Metalama.Framework.Options.IHierarchicalOptionsProvider"/>.
    /// See https://github.com/metalama/Metalama/issues/2235.
    /// </remarks>
    private Type? GetTypeFromDomain( AssemblyIdentity assemblyIdentity, string reflectionName )
    {
        var domain = this._projectRepository.Domain;

        if ( domain == null || !domain.TryGetLoadedAssembly( new AssemblyName( assemblyIdentity.GetDisplayName() ), out var assembly ) )
        {
            return null;
        }

        return assembly.GetType( reflectionName, false );
    }
}