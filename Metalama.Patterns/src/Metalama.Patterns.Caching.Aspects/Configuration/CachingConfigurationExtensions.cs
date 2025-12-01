// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

/// <summary>
/// Extension methods for configuring caching options programmatically through fabrics.
/// </summary>
/// <seealso cref="CachingOptionsBuilder"/>
/// <seealso cref="CachingConfigurationAttribute"/>
/// <seealso href="@caching-configuration"/>
[PublicAPI]
[CompileTime]
public static class CachingConfigurationExtensions
{
    /// <summary>
    /// Configures caching options for the selected methods.
    /// </summary>
    /// <param name="method">The query selecting the methods to configure.</param>
    /// <param name="build">A delegate that configures the <see cref="CachingOptionsBuilder"/>.</param>
    public static void ConfigureCaching( this IQuery<IMethod> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }

    /// <summary>
    /// Configures default caching options for all methods in the compilation.
    /// </summary>
    /// <param name="method">The query selecting the compilation.</param>
    /// <param name="build">A delegate that configures the <see cref="CachingOptionsBuilder"/>.</param>
    public static void ConfigureCaching( this IQuery<ICompilation> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }

    /// <summary>
    /// Configures default caching options for all methods in the selected namespaces.
    /// </summary>
    /// <param name="method">The query selecting the namespaces.</param>
    /// <param name="build">A delegate that configures the <see cref="CachingOptionsBuilder"/>.</param>
    public static void ConfigureCaching( this IQuery<INamespace> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }

    /// <summary>
    /// Configures default caching options for all methods in the selected types.
    /// </summary>
    /// <param name="method">The query selecting the types.</param>
    /// <param name="build">A delegate that configures the <see cref="CachingOptionsBuilder"/>.</param>
    public static void ConfigureCaching( this IQuery<INamedType> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }
}