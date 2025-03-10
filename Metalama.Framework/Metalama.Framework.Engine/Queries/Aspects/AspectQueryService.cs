// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Fabrics;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries.Aspects;

internal sealed class AspectQueryService : IAspectQueryService
{
    private readonly ILogger _logger;

    public AspectQueryService( ProjectServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "QueryAspectService" );
    }

    private static AspectClass? GetAspectClass<TDeclaration>( IQueryImpl<TDeclaration> query, Type aspectType )
        where TDeclaration : class, IDeclaration
    {
        if ( !query.Owner.AspectClasses.TryGetAspectClass( aspectType, out var aspectClass ) )
        {
            return null;
        }

        if ( aspectClass.IsAbstract )
        {
            throw new ArgumentOutOfRangeException( nameof(aspectType), MetalamaStringFormatter.Format( $"'{aspectType}' is an abstract type." ) );
        }

        return (AspectClass) aspectClass;
    }

    private static void RegisterAspectSource<TDeclaration>( IQueryImpl<TDeclaration> query, IAspectSource aspectSource )
        where TDeclaration : class, IDeclaration
    {
        query.OnChildAdded();

        query.Owner.AddContributor( aspectSource );
    }

    public void AddAspect<TDeclaration>( IQuery<TDeclaration> query, Type aspectType, Func<TDeclaration, IAspect> createAspect )
        where TDeclaration : class, IDeclaration
        => this.AddAspectImpl( (IQueryImpl<TDeclaration>) query, aspectType, ( declaration, _ ) => createAspect( declaration ), null );

    public void AddAspect<TDeclaration, TTag>( ITaggedQuery<TDeclaration, TTag> query, Type aspectType, Func<TDeclaration, TTag, IAspect> createAspect )
        where TDeclaration : class, IDeclaration
        => this.AddAspectImpl( (IQueryImpl<TDeclaration>) query, aspectType, ( declaration, tag ) => createAspect( declaration, (TTag) tag! ), null );

    public void RequireAspect<TDeclaration>( IQuery<TDeclaration> query, Type aspectType )
        where TDeclaration : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<TDeclaration>) query;

        var aspectClass = GetAspectClass( queryImpl, aspectType );

        if ( aspectClass == null )
        {
            // The aspect class was not found. We're going to assume that this happened because of an already-reported error and do nothing.

            this._logger.Warning?.Log( $"The aspect type '{aspectType.Namespace}' was not found when calling RequireAspect." );

            return;
        }

        RegisterAspectSource(
            queryImpl,
            new AspectQuerySource<TDeclaration>(
                aspectClass,
                queryImpl,
                ( declaration, _, _, collector ) =>
                {
                    collector.AddAspectRequirement(
                        new AspectRequirement(
                            declaration.ToRef(),
                            queryImpl.Owner.AspectPredecessor.Instance ) );

                    return Task.CompletedTask;
                },
                null ) );
    }

    public void AddAspectIfEligible<TDeclaration>(
        IQuery<TDeclaration> query,
        Type aspectType,
        Func<TDeclaration, IAspect> createAspect,
        EligibleScenarios eligibility )
        where TDeclaration : class, IDeclaration
        => this.AddAspectImpl( (IQueryImpl<TDeclaration>) query, aspectType, ( declaration, _ ) => createAspect( declaration ), eligibility );

    public void AddAspectIfEligible<TDeclaration, TTag>(
        ITaggedQuery<TDeclaration, TTag> query,
        Type aspectType,
        Func<TDeclaration, TTag, IAspect> createAspect,
        EligibleScenarios eligibility )
        where TDeclaration : class, IDeclaration
        => this.AddAspectImpl( (IQueryImpl<TDeclaration>) query, aspectType, ( declaration, tag ) => createAspect( declaration, (TTag) tag! ), eligibility );

    private void AddAspectImpl<TDeclaration>(
        IQueryImpl<TDeclaration> query,
        Type aspectType,
        Func<TDeclaration, object?, IAspect> createAspect,
        EligibleScenarios? eligibleScenarios )
        where TDeclaration : class, IDeclaration
    {
        var aspectClass = GetAspectClass( query, aspectType );

        if ( aspectClass == null )
        {
            // The aspect class was not found. We're going to assume that this happened because of an already-reported error and do nothing.

            this._logger.Warning?.Log( $"The aspect type {aspectType} was not found when calling AddAspectIfEligible." );

            return;
        }

        RegisterAspectSource(
            query,
            new AspectQuerySource<TDeclaration>(
                aspectClass,
                query,
                ( declaration, tag, context, collector ) =>
                {
                    if ( context.UserCodeInvoker.TryInvoke(
                            () => createAspect( declaration, tag ),
                            context.UserCodeExecutionContext.AssertNotNull(),
                            out var aspect ) )
                    {
                        collector.AddAspectInstance(
                            new AspectInstance(
                                aspect,
                                declaration,
                                aspectClass,
                                query.Owner.AspectPredecessor ) );
                    }

                    return Task.CompletedTask;
                },
                eligibleScenarios ) );
    }
}