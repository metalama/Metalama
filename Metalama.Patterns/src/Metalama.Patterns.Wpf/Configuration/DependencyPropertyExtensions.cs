// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Wpf.Configuration;

[PublicAPI]
[CompileTime]
public static class DependencyPropertyExtensions
{
    /// <summary>
    /// Configures <see cref="DependencyPropertyAttribute"/> for the current project.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current compilation.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureDependencyProperty(
        this IQuery<ICompilation> query,
        Action<DependencyPropertyOptionsBuilder> configure )
    {
        var builder = new DependencyPropertyOptionsBuilder();
        configure( builder );

        var options = builder.Build();
        query.SetOptions( options );
    }

    /// <summary>
    /// Configures <see cref="DependencyPropertyAttribute"/> for the current namespace.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current namespace.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureDependencyProperty(
        this IQuery<INamespace> query,
        Action<DependencyPropertyOptionsBuilder> configure )
    {
        var builder = new DependencyPropertyOptionsBuilder();
        configure( builder );

        var options = builder.Build();
        query.SetOptions( options );
    }

    /// <summary>
    /// Configures <see cref="DependencyPropertyAttribute"/> for the current type.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current type.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureDependencyProperty(
        this IQuery<INamedType> query,
        Action<DependencyPropertyOptionsBuilder> configure )
    {
        var builder = new DependencyPropertyOptionsBuilder();
        configure( builder );

        var options = builder.Build();
        query.SetOptions( options );
    }

    /// <summary>
    /// Configures <see cref="DependencyPropertyAttribute"/> for the current property.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current property.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureDependencyProperty(
        this IQuery<IProperty> query,
        Action<DependencyPropertyOptionsBuilder> configure )
    {
        var builder = new DependencyPropertyOptionsBuilder();
        configure( builder );

        var options = builder.Build();
        query.SetOptions( options );
    }
}