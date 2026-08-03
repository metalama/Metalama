// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;

namespace Metalama.Framework.Engine.Services;

public static class ServiceProviderExtensions
{
    public static ILoggerFactory GetLoggerFactory( this ProjectServiceProvider serviceProvider ) => serviceProvider.Underlying.GetLoggerFactory();

    public static ILoggerFactory GetLoggerFactory( this GlobalServiceProvider serviceProvider ) => serviceProvider.Underlying.GetLoggerFactory();

    /// <summary>
    /// Gets the global <see cref="CompileTimeAssemblyLocator"/>, but initialize it with the current <see cref="ProjectServiceProvider"/> if it has not
    /// been initialized yet.
    /// </summary>
    internal static CompileTimeAssemblyLocator GetReferenceAssemblyLocator( this ProjectServiceProvider serviceProvider )
        => serviceProvider.Global.GetRequiredService<ICompileTimeAssemblyLocatorProvider>().GetInstance( serviceProvider );

    /// <summary>
    /// Resolves the compile-time reference assemblies of a project, and reports to <paramref name="diagnostics"/> and
    /// returns <c>false</c> when they cannot be resolved.
    /// </summary>
    /// <remarks>
    /// Resolving them runs a nested build, which fails for reasons that belong to the environment rather than to
    /// Metalama. A pipeline calls this method at its entry point, where it has a diagnostic sink, so that the failure
    /// becomes one of its own diagnostics. Every later use of the locator in the same project then finds it already
    /// resolved. See issue #1744.
    /// </remarks>
    public static bool TryResolveReferenceAssemblies( this ProjectServiceProvider serviceProvider, IDiagnosticAdder diagnostics )
        => serviceProvider.Global.GetRequiredService<ICompileTimeAssemblyLocatorProvider>().TryGetInstance( serviceProvider, diagnostics, out _ );

    public static T GetRequiredBackstageService<T>( this GlobalServiceProvider serviceProvider )
        where T : class, IBackstageService
        => serviceProvider.Underlying.GetRequiredBackstageService<T>();

    internal static T? GetBackstageService<T>( this GlobalServiceProvider serviceProvider )
        where T : class, IBackstageService
        => serviceProvider.Underlying.GetBackstageService<T>();
}