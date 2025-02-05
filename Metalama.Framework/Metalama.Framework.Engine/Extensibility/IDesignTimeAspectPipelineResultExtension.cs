// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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