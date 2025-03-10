// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Services;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Services;

internal sealed class SourceGeneratorDetectionService( in ProjectServiceProvider serviceProvider ) : ISourceGeneratorDetectionService
{
    private readonly ImmutableArray<string> _sourceGeneratorAttributes = serviceProvider.GetRequiredService<IProjectOptions>().SourceGeneratorAttributes;

    public bool IsWellKnownGeneratedDeclaration( IMember member )
        => member.IsPartial && this._sourceGeneratorAttributes.Any( sga => member.Attributes.Any( da => da.Type.FullName == sga ) );
}