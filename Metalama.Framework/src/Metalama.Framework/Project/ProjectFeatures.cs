// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Project;

/// <summary>
/// Exposes capabilities of the target project based on the target framework(s) and language version.
/// </summary>
[CompileTime]
public abstract class ProjectFeatures
{
    /// <summary>
    /// Gets a value indicating whether the target runtime supports covariant return types in method overrides.
    /// This requires .NET 5.0+ runtime and C# 9.0+ language version. When the project targets multiple
    /// frameworks, this returns <c>false</c> if any target does not support the feature.
    /// </summary>
    public abstract bool SupportsCovariantReturnTypes { get; }
}
