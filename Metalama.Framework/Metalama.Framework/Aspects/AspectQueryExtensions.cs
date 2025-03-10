// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Aspects;

[CompileTime]
[PublicAPI]
public static class AspectQueryExtensions
{
    // NOTE: AddAspect<TAspect>() AddAspectIfEligible<TAspect>(), and RequireAspect<TAspect>() intentionally omit the TDeclaration generic parameter
    // for the convenience of the caller, but at the cost of ignoring type safety because we drop the requirement that the aspect is compatible with
    // the type of items in the queries. The alternative is to specify TDeclaration, but then it must be specified by the caller, which is redundant
    // and not backward-compatible. 

    /// <summary>
    /// Adds a aspect to the current set of declarations or throws an exception if the aspect is not eligible for the aspect. This overload is non-generic.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param>
    /// <param name="aspectType">The exact type of the aspect returned by <paramref name="createAspect"/>. It is not allowed to specify a base type in this parameter, only the exact type.</param>
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    public static void AddAspect<TDeclaration>( this IQuery<TDeclaration> query, Type aspectType, Func<TDeclaration, IAspect> createAspect )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspect( query, aspectType, createAspect );

    /// <summary>
    /// Adds an aspect to the current set of declarations but only if the aspect is eligible for the declaration. This overload is non-generic.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param>
    /// <param name="aspectType">The exact type of the aspect returned by <paramref name="createAspect"/>. It is not allowed to specify a base type in this parameter, only the exact type.</param>
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    /// <param name="eligibility">The scenarios for which the aspect may be eligible. The default value is <see cref="EligibleScenarios.Default"/> | <see cref="EligibleScenarios.Inheritance"/>.
    /// If <see cref="EligibleScenarios.None"/> is provided, eligibility is not checked.
    /// </param>
    public static void AddAspectIfEligible<TDeclaration>(
        this IQuery<TDeclaration> query,
        Type aspectType,
        Func<TDeclaration, IAspect> createAspect,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspectIfEligible( query, aspectType, createAspect, eligibility );

    /// <summary>
    /// Adds an aspect to the current set of declarations or throws an exception if the aspect is not eligible for the aspect.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param>
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    public static void AddAspect<TDeclaration, TAspect>( this IQuery<TDeclaration> query, Func<TDeclaration, TAspect> createAspect )
        where TDeclaration : class, IDeclaration
        where TAspect : class, IAspect<TDeclaration>
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspect( query, typeof(TAspect), createAspect );

    /// <summary>
    /// Adds an aspect to the current set of declarations but only if the aspect is eligible for the declaration. 
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param>
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    /// <param name="eligibility">The scenarios for which the aspect may be eligible. The default value is <see cref="EligibleScenarios.Default"/> | <see cref="EligibleScenarios.Inheritance"/>.
    /// If <see cref="EligibleScenarios.None"/> is provided, eligibility is not checked.
    /// </param>
    public static void AddAspectIfEligible<TDeclaration, TAspect>(
        this IQuery<TDeclaration> query,
        Func<TDeclaration, TAspect> createAspect,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TDeclaration : class, IDeclaration
        where TAspect : class, IAspect<TDeclaration>
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspectIfEligible( query, typeof(TAspect), createAspect, eligibility );

    /// <summary>
    /// Adds an aspect to the current set of declarations or throws an exception if the aspect is not eligible for the aspect. This overload creates a new instance of the
    /// aspect class for each target declaration.
    /// </summary>
    public static void AddAspect<TAspect>( this IQuery<IDeclaration> query )
        where TAspect : class, IAspect, new()
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspect( query, typeof(TAspect), _ => new TAspect() );

    /// <summary>
    /// Adds an aspect to the current set of declarations using the default constructor of the aspect type. This method
    /// does not verify the eligibility of the declaration for the aspect unless you specify the <paramref name="eligibility"/> parameter.
    /// This overload creates a new instance of the aspect class for each eligible target declaration.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param>
    /// <param name="eligibility">The scenarios for which the aspect may be eligible. The default value is <see cref="EligibleScenarios.Default"/> | <see cref="EligibleScenarios.Inheritance"/>.
    /// If <see cref="EligibleScenarios.None"/> is provided, eligibility is not checked.
    /// </param>
    public static void AddAspectIfEligible<TAspect>(
        this IQuery<IDeclaration> query,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TAspect : class, IAspect, new()
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>()
            .AddAspectIfEligible( query, typeof(TAspect), _ => new TAspect(), eligibility );

    /// <summary>
    /// Requires an instance of a specified aspect type to be present on a specified declaration. If the aspect
    /// is not present, this method adds a new instance of the aspect by using the default aspect constructor. 
    /// </summary>
    /// <remarks>
    /// <para>Calling this method causes the current aspect to be present in the <see cref="IAspectPredecessor.Predecessors"/> list
    /// even if the required aspect was already present on the target declaration.</para>
    /// </remarks>
    /// <typeparam name="TAspect">Type of the aspect. The type must be ordered after the aspect type calling this method.</typeparam>
    public static void RequireAspect<TAspect>( this IQuery<IDeclaration> query )
        where TAspect : class, IAspect, new()
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().RequireAspect( query, typeof(TAspect) );

    /// <summary>
    /// Adds a aspect to the current set of declarations or throws an exception if the aspect is not eligible for the aspect. This overload is non-generic.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param>
    /// <param name="aspectType">The exact type of the aspect returned by <paramref name="createAspect"/>. It is not allowed to specify a base type in this parameter, only the exact type.</param>
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    public static void AddAspect<TDeclaration, TTag>(
        this ITaggedQuery<TDeclaration, TTag> query,
        Type aspectType,
        Func<TDeclaration, TTag, IAspect> createAspect )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspect( query, aspectType, createAspect );

    /// <summary>
    /// Adds an aspect to the current set of declarations but only if the aspect is eligible for the declaration. This overload is non-generic.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="aspectType">The exact type of the aspect returned by <paramref name="createAspect"/>. It is not allowed to specify a base type in this parameter, only the exact type.</param>
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    /// <param name="eligibility">The scenarios for which the aspect may be eligible. The default value is <see cref="EligibleScenarios.Default"/> | <see cref="EligibleScenarios.Inheritance"/>.
    /// If <see cref="EligibleScenarios.None"/> is provided, eligibility is not checked.
    /// </param>
    public static void AddAspectIfEligible<TDeclaration, TTag>(
        this ITaggedQuery<TDeclaration, TTag> query,
        Type aspectType,
        Func<TDeclaration, TTag, IAspect> createAspect,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspectIfEligible( query, aspectType, createAspect, eligibility );

    /// <summary>
    /// Adds an aspect to the current set of declarations or throws an exception if the aspect is not eligible for the aspect.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    public static void AddAspect<TDeclaration, TTag, TAspect>( this ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, TAspect> createAspect )
        where TDeclaration : class, IDeclaration
        where TAspect : class, IAspect<TDeclaration>
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspect( query, typeof(TAspect), createAspect );

    /// <summary>
    /// Adds an aspect to the current set of declarations but only if the aspect is eligible for the declaration. 
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="createAspect">A function that returns the aspect for a given declaration.</param>
    /// <param name="eligibility">The scenarios for which the aspect may be eligible. The default value is <see cref="EligibleScenarios.Default"/> | <see cref="EligibleScenarios.Inheritance"/>.
    /// If <see cref="EligibleScenarios.None"/> is provided, eligibility is not checked.
    /// </param>
    public static void AddAspectIfEligible<TDeclaration, TTag, TAspect>(
        this ITaggedQuery<TDeclaration, TTag> query,
        Func<TDeclaration, TTag, TAspect> createAspect,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TDeclaration : class, IDeclaration
        where TAspect : class, IAspect<TDeclaration>
        => query.Project.ServiceProvider.GetRequiredService<IAspectQueryService>().AddAspectIfEligible( query, typeof(TAspect), createAspect, eligibility );
}