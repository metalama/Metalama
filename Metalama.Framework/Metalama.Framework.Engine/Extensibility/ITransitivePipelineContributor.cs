// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents a transitive (cross-project) pipeline contributor. Typically a reference validator.
/// </summary>
public interface ITransitivePipelineContributor
{
    SyntaxTree? SyntaxTree { get; }

    IDesignTimeAspectPipelineResultExtension? ToDesignTime();
}