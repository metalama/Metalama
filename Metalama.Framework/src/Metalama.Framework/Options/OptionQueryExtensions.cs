// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Options;

[CompileTime]
[PublicAPI]
public static class OptionQueryExtensions
{
    /// <summary>
    /// Sets options for the declarations in the current set of declarations by supplying a <see cref="Func{TResult}"/>.
    /// </summary>
    /// <param name="query">The declarations for which the options must be set.</param>
    /// <param name="getOptions">A function giving the options for the given declaration.</param>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <typeparam name="TDeclaration">The type of declarations selected by the query.</typeparam>
    /// <remarks>
    /// This method should only set the option properties that need to be changed. All unchanged properties must be let null.
    /// </remarks>
    public static void SetOptions<TDeclaration, TOptions>( this IQuery<TDeclaration> query, Func<TDeclaration, TOptions> getOptions )
        where TDeclaration : class, IDeclaration
        where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new()
        => query.Project.ServiceProvider.GetRequiredService<IOptionQueryService>().SetOptions( query, getOptions );

    /// <summary>
    /// Sets options for the declarations in the current set of declarations by supplying a <see cref="Func{TResult}"/>.
    /// </summary>
    /// <param name="query">The declarations for which the options must be set.</param> 
    /// <param name="options">The options.</param>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <typeparam name="TDeclaration">The type of declarations selected by the query.</typeparam>
    /// <remarks>
    /// This method should only set the option properties that need to be changed. All unchanged properties must be let null.
    /// </remarks>
    public static void SetOptions<TDeclaration, TOptions>( this IQuery<TDeclaration> query, TOptions options )
        where TDeclaration : class, IDeclaration
        where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new()
        => query.Project.ServiceProvider.GetRequiredService<IOptionQueryService>().SetOptions( query, _ => options );

    /// <summary>
    /// Sets options for the declarations in the current set of declarations by supplying a <see cref="Func{TResult}"/>.
    /// </summary>
    /// <param name="query">The declarations for which the options must be set.</param> 
    /// <param name="getOptions">A function giving the options for the given declaration.</param>
    /// <typeparam name="TDeclaration">The type of declarations selected by the query.</typeparam> 
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <typeparam name="TTag">The type of the tag.</typeparam>
    /// <remarks>
    /// This method should only set the option properties that need to be changed. All unchanged properties must be let null.
    /// </remarks>
    public static void SetOptions<TDeclaration, TTag, TOptions>( this ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, TOptions> getOptions )
        where TDeclaration : class, IDeclaration
        where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new()
        => query.Project.ServiceProvider.GetRequiredService<IOptionQueryService>().SetOptions( query, getOptions );
}