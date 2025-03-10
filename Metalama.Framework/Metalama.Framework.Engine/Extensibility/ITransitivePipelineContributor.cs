// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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