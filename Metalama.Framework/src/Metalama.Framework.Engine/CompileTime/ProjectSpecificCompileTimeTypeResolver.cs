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
        => this.Cache.GetOrAdd( typeSymbol, this.GetCompileTimeNamedTypeCore, cancellationToken );

    private Type? GetCompileTimeNamedTypeCore( ITypeSymbol typeSymbol, CancellationToken cancellationToken )
    {
        // Check if the type belongs to an assembly that is loaded into the compile-time domain. This must be done before
        // the SystemTypeResolver is called, because the domain may hold a copy of an assembly that the SystemTypeResolver
        // would load again.
        var domainType = this.GetTypeFromDomain( typeSymbol );

        if ( domainType != null )
        {
            return domainType;
        }

        // Check if the type is a .NET system one.
        var systemType = this._systemTypeResolver.GetCompileTimeType( typeSymbol, false, cancellationToken );

        if ( systemType != null )
        {
            return systemType;
        }

        // The type is not a system one. Check if it is a compile-time one.
        if ( !this._projectRepository.TryGetCompileTimeProject( typeSymbol.ContainingAssembly.Identity, out var compileTimeProject ) )
        {
            return null;
        }

        return compileTimeProject?.GetTypeOrNull( typeSymbol.GetReflectionFullName() );
    }

    /// <summary>
    /// Gets a type of an assembly that is loaded into the <see cref="CompileTimeDomain"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An assembly that is referenced with the <c>MetalamaCompileTimeAssembly</c> item is a standard assembly, so it has no compile-time project.
    /// When it is also an extension assembly, it is loaded into the <see cref="CompileTimeDomain"/>, and the compile-time code and the extensions
    /// use the types of this copy. This is the case of the public assembly of a Metalama extension package, which can define a custom attribute
    /// that the pipeline instantiates, for instance an <see cref="Metalama.Framework.Options.IHierarchicalOptionsProvider"/>.
    /// </para>
    /// <para>
    /// In the compiler process, the <see cref="SystemTypeResolver"/> does not find the types of such an assembly. In the process of the aspect
    /// test runner, the assembly is also a dependency of the test project, so the <see cref="SystemTypeResolver"/> returns the type of the copy
    /// in the default <c>AssemblyLoadContext</c>. An object created from this type cannot be cast to the type of the same name that the
    /// compile-time code and the extensions use. Therefore, the copy in the domain takes precedence.
    /// </para>
    /// <para>
    /// See https://github.com/metalama/Metalama/issues/2235.
    /// </para>
    /// </remarks>
    private Type? GetTypeFromDomain( ITypeSymbol typeSymbol )
    {
        var domain = this._projectRepository.Domain;

        if ( domain == null
             || typeSymbol.ContainingAssembly is not { } assemblySymbol
             || !domain.TryGetLoadedAssembly( new AssemblyName( assemblySymbol.Identity.GetDisplayName() ), out var assembly ) )
        {
            return null;
        }

        return assembly.GetType( typeSymbol.GetReflectionFullName(), false );
    }
}