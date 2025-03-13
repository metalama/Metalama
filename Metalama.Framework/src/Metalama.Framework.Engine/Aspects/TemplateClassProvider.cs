// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class TemplateClassProvider : IProjectService
{
    private readonly ImmutableDictionary<string, TemplateClass> _templateClasses;

    public TemplateClassProvider( ImmutableDictionary<string, TemplateClass> templateClasses )
    {
        this._templateClasses = templateClasses;
    }

    public TemplateClass Get( TemplateProvider templateProvider ) => this._templateClasses[templateProvider.Type.AssertNotNull().FullName.AssertNotNull()];
}