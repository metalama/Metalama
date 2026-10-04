// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// Implements the verbs of the proof of concept with the public extension points of the engine.
/// </summary>
internal sealed class TestExtensionPointsService : ITestExtensionPointsService
{
    /// <summary>
    /// Adds a <see cref="TestExtensionPipelineContributor"/> whose channel is <c>adviser</c> to the owner of an adviser. When
    /// <paramref name="expectedTemplateProvider"/> is not <c>null</c>, the registration records whether the template provider of the adviser
    /// is the expected one.
    /// </summary>
    public void Register<T>( IAdviser<T> adviser, string tag, ITemplateProvider? expectedTemplateProvider )
        where T : class, IDeclaration
    {
        var context = adviser.GetExtensionContext();
        context.ThrowIfDisposed();

        bool? templateProviderMatches = null;

        if ( expectedTemplateProvider != null )
        {
            templateProviderMatches = context.TemplateProvider == TemplateProvider.FromInstance( expectedTemplateProvider );
        }

        context.QueryOwner.AddContributor( new TestExtensionPipelineContributor( tag, context.CaptureOrigin(), adviser.Target.ToRef(), "adviser", templateProviderMatches ) );
    }

    /// <summary>
    /// Creates a query that selects the target of an adviser, with <see cref="AdviserExtensionContext.CreateQuery{T}"/>, and registers a
    /// contribution through this query.
    /// </summary>
    public void RegisterThroughCreatedQuery<T>( IAdviser<T> adviser, string tag )
        where T : class, IDeclaration
        => this.Register( adviser.GetExtensionContext().CreateQuery( adviser.Target ), tag );

    /// <summary>
    /// Adds a <see cref="TestReferenceReport"/> to the owner of an adviser. When <paramref name="restrictToTarget"/> is <c>true</c>, the syntax
    /// of the target of the adviser gives the declaration roots of the shared index.
    /// </summary>
    public void ReportReferences<T>( IAdviser<T> adviser, string methodName, bool restrictToTarget )
        where T : class, IDeclaration
    {
        var context = adviser.GetExtensionContext();
        context.ThrowIfDisposed();

        ImmutableArray<SyntaxNode>? roots = restrictToTarget
            ? adviser.Target.Sources.Select( s => s.SyntaxNodeOrToken().AsNode() ).OfType<SyntaxNode>().ToImmutableArray()
            : null;

        context.QueryOwner.AddContributor( new TestReferenceReport( methodName, roots ) );
    }

    /// <summary>
    /// Adds a <see cref="TestRedirection"/> to the owner of an adviser. The target of the adviser is the scope of the redirection, and its syntax
    /// gives the declaration roots of the shared index.
    /// </summary>
    public void RedirectCalls<T>( IAdviser<T> adviser, string methodName, IMethod replacement, TestRedirectionOptions options )
        where T : class, IDeclaration
    {
        var context = adviser.GetExtensionContext();
        context.ThrowIfDisposed();

        var roots = adviser.Target.Sources.Select( s => s.SyntaxNodeOrToken().AsNode() ).OfType<SyntaxNode>().ToImmutableArray();

        context.QueryOwner.AddContributor(
            new TestRedirection( methodName, context.CaptureOrigin(), options, replacement.ToRef(), null, adviser.Target.ToRef(), null, roots ) );
    }

    /// <summary>
    /// Adds a <see cref="TestRedirection"/> to the owner of a query. The query is the scope of the redirection, and the replacement method is
    /// given by name.
    /// </summary>
    public void RedirectCalls<T>( IQuery<T> query, string methodName, string replacementTypeName, string replacementMethodName, TestRedirectionOptions options )
        where T : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<T>) query;
        queryImpl.OnChildAdded();

        var origin = ExtensionContributionOrigin.Capture( queryImpl.Owner );

        queryImpl.Owner.AddContributor(
            new TestRedirection( methodName, origin, options, null, (replacementTypeName, replacementMethodName), null, queryImpl, null ) );
    }

    public void Register<T>( IQuery<T> query, string tag )
        where T : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<T>) query;
        queryImpl.OnChildAdded();

        var origin = ExtensionContributionOrigin.Capture( queryImpl.Owner );

        queryImpl.Owner.AddContributor( new TestExtensionPipelineContributor( tag, origin, null, "query", null ) );
    }
}
