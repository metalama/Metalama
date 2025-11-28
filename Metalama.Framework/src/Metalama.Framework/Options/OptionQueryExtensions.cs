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

/// <summary>
/// Extension methods for <see cref="IQuery{T}"/> and <see cref="ITaggedQuery{T, TTag}"/> to set hierarchical options.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods allow fabrics and aspects to configure hierarchical options for declarations selected by queries.
/// This is the primary mechanism for applying options at scale across a project, namespace, or type family.
/// </para>
/// <para>
/// The <see cref="SetOptions{TDeclaration, TOptions}(IQuery{TDeclaration}, TOptions)"/> methods are typically called from
/// fabric classes (such as <see cref="ProjectFabric"/>, <see cref="NamespaceFabric"/>, or <see cref="TypeFabric"/>) on the
/// <see cref="IAmender{T}.Outbound"/> property to configure aspects throughout the codebase.
/// </para>
/// <para>
/// <b>Builder Pattern for Better User Experience:</b> For complex option classes, aspect authors can improve the user experience
/// by providing a builder pattern. Create custom extension methods (e.g., <c>ConfigureMyAspect(this IQuery&lt;T&gt; query, Action&lt;MyOptionsBuilder&gt; configure)</c>)
/// that accept a configuration delegate. The extension method creates a builder instance, invokes the delegate to configure it,
/// calls <c>Build()</c> to produce the options object, and finally calls <see cref="SetOptions{TDeclaration, TOptions}(IQuery{TDeclaration}, TOptions)"/>.
/// This allows users to write fluent configuration code without dealing with nullable properties directly.
/// For a complete example, see Metalama.Extensions.DependencyInjection:
/// <list type="bullet">
/// <item><description>Options class: <see href="https://github.com/metalama/Metalama/blob/HEAD/Metalama.Extensions/src/Metalama.Extensions.DependencyInjection/Implementation/DependencyInjectionOptions.cs"/></description></item>
/// <item><description>Builder class: <see href="https://github.com/metalama/Metalama/blob/HEAD/Metalama.Extensions/src/Metalama.Extensions.DependencyInjection/DependencyInjectionOptionsBuilder.cs"/></description></item>
/// <item><description>Extension methods: <see href="https://github.com/metalama/Metalama/blob/HEAD/Metalama.Extensions/src/Metalama.Extensions.DependencyInjection/DependencyInjectionExtensions.cs"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso href="@exposing-options"/>
/// <seealso href="@fabrics"/>
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