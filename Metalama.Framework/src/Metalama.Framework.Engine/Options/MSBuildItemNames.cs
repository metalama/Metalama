// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable InconsistentNaming

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Options;

[PublicAPI]
public static class MSBuildItemNames
{
    public const string MetalamaCompileTimePackage = nameof(MetalamaCompileTimePackage);

    public const string MetalamaCompileTimeAssembly = nameof(MetalamaCompileTimeAssembly);

    public const string MetalamaSourceGeneratorAttribute = nameof(MetalamaSourceGeneratorAttribute);

    /// <summary>
    /// List of compile-time extension assemblies. Passed through the <see cref="MSBuildPropertyNames.MetalamaExtensionAssemblies"/> property.
    /// </summary>
    public const string MetalamaExtensionAssembly = nameof(MetalamaExtensionAssembly);

    /// <summary>
    /// List of design-time extension assemblies. Passed through the <see cref="MSBuildPropertyNames.MetalamaDesignTimeExtensionAssemblies"/> property.
    /// </summary>
    public const string MetalamaDesignTimeExtensionAssembly = nameof(MetalamaDesignTimeExtensionAssembly);
}