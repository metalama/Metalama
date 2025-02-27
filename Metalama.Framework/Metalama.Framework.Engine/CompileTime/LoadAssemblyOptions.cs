// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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