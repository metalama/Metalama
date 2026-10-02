// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Extensibility;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// Implemented by an <see cref="IDesignTimePipelineResultExtension"/> that needs references of the analyzed file at design time.
/// </summary>
/// <remarks>
/// The requirements are added to <see cref="DesignTimeAspectPipelineResultExtensionCollection.IndexOptions"/> of the project. They are not merged into
/// the options of referencing projects.
/// </remarks>
[PublicAPI]
public interface IDesignTimeReferenceIndexRequirementsProvider
{
    /// <summary>
    /// Gets the requirements of the extension.
    /// </summary>
    IEnumerable<ReferenceIndexerRequirements> ReferenceIndexerRequirements { get; }
}
