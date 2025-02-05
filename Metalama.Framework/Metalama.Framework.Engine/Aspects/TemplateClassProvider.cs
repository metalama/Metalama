// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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