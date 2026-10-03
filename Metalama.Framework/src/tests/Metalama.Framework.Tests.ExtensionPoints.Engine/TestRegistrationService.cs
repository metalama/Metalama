// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// Implements the verbs of the proof of concept with the public extension points of the engine.
/// </summary>
internal sealed class TestRegistrationService : ITestRegistrationService
{
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

        context.Owner.AddContributor( new TestRegistration( tag, context.CaptureOrigin(), adviser.Target.ToRef(), "adviser", templateProviderMatches ) );
    }

    public void Register<T>( IQuery<T> query, string tag )
        where T : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<T>) query;
        queryImpl.OnChildAdded();

        var origin = ExtensionContributionOrigin.Capture( queryImpl.Owner );

        queryImpl.Owner.AddContributor( new TestRegistration( tag, origin, null, "query", null ) );
    }
}
