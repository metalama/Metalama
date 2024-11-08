// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Services;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Services;

internal class SourceGeneratorDetectionService( in ProjectServiceProvider serviceProvider ) : ISourceGeneratorDetectionService
{
    private readonly ImmutableArray<string> _sourceGeneratorAttributes = serviceProvider.GetRequiredService<IProjectOptions>().SourceGeneratorAttributes;

    public bool IsWellKnownGeneratedDeclaration( IMember member )
        => member.IsPartial && this._sourceGeneratorAttributes.Any( sga => member.Attributes.Any( da => da.Type.FullName == sga ) );
}