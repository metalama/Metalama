// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Aspects;

internal interface IAspectQueryService : IProjectService
{
    void AddAspect<TDeclaration>( IQuery<TDeclaration> query, Type aspectType, Func<TDeclaration, IAspect> createAspect )
        where TDeclaration : class, IDeclaration;

    void AddAspectIfEligible<TDeclaration>(
        IQuery<TDeclaration> query,
        Type aspectType,
        Func<TDeclaration, IAspect> createAspect,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TDeclaration : class, IDeclaration;

    void AddAspect<TDeclaration, TTag>( ITaggedQuery<TDeclaration, TTag> query, Type aspectType, Func<TDeclaration, TTag, IAspect> createAspect )
        where TDeclaration : class, IDeclaration;

    void AddAspectIfEligible<TDeclaration, TTag>(
        ITaggedQuery<TDeclaration, TTag> query,
        Type aspectType,
        Func<TDeclaration, TTag, IAspect> createAspect,
        EligibleScenarios eligibility = EligibleScenarios.Default | EligibleScenarios.Inheritance )
        where TDeclaration : class, IDeclaration;

    void RequireAspect<TDeclaration>( IQuery<TDeclaration> query, Type aspectType )
        where TDeclaration : class, IDeclaration;
}