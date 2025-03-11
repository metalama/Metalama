// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.CompileTime;

public record struct LoadAssemblyOptions
{
    public static LoadAssemblyOptions Default => default;

    public static LoadAssemblyOptions Shared => new() { IsShared = true };

    /// <summary>
    /// Gets a value indicating whether the assembly is shared by several projects, in which case it is NOT supposed
    /// to be collectible after the <see cref="CompileTimeDomain"/> is disposed of.
    /// This should be <c>false</c> for project-specific assemblies and <c>true</c> for extensions.
    /// </summary>
    public bool IsShared { get; init; }

    /// <summary>
    /// Gets a value indicating whether the assembly must be first loaded into a buffer to avoid locking the file.
    /// </summary>
    public bool AvoidLocking { get; init; }
}