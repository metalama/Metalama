// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Provides access to SDK utilities that require version-specific implementations.
/// This interface is implemented in the Engine assembly and initialized early in the execution.
/// </summary>
internal interface IMetalamaEngineServices
{
    /// <summary>
    /// Gets the <see cref="IRoslynExtensions"/> implementation for the current Roslyn version.
    /// </summary>
    IRoslynExtensions RoslynExtensions { get; }
}
