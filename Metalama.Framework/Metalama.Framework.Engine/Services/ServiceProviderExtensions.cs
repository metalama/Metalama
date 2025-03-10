// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Framework.Engine.CompileTime;

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

    public static T GetRequiredBackstageService<T>( this GlobalServiceProvider serviceProvider )
        where T : class, IBackstageService
        => serviceProvider.Underlying.GetRequiredBackstageService<T>();

    internal static T? GetBackstageService<T>( this GlobalServiceProvider serviceProvider )
        where T : class, IBackstageService
        => serviceProvider.Underlying.GetBackstageService<T>();
}