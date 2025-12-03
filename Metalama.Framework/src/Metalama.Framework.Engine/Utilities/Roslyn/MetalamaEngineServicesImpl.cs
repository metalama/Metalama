// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Implementation of <see cref="IMetalamaEngineServices"/> that provides access to Engine utilities.
/// </summary>
internal sealed class MetalamaEngineServicesImpl : IMetalamaEngineServices
{
    /// <summary>
    /// Gets the singleton instance of the execution context.
    /// </summary>
    public static MetalamaEngineServicesImpl Instance { get; } = new();

    private MetalamaEngineServicesImpl() { }

    /// <inheritdoc />
    public IRoslynExtensions RoslynExtensions => RoslynExtensionsImpl.Instance;
}