// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Utilities.Roslyn;

namespace Metalama.Framework.Engine.Extensibility;

public interface IDesignTimeAspectPipelineResultExtension
{
    ReferenceIndexerRequirements ReferenceIndexerRequirements { get; }

    SymbolDictionaryKey ValidatedDeclaration { get; }

    ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension();
}