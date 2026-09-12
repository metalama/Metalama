// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.IO;

namespace Metalama.Backstage.Configuration;

[AttributeUsage( AttributeTargets.Class )]
[PublicAPI]
public class ConfigurationFileAttribute : Attribute
{
    public ConfigurationFileAttribute( string fileName, string? alias = null )
    {
        this.FileName = fileName;
        this.Alias = alias ?? Path.GetFileNameWithoutExtension( fileName );
    }

    public string FileName { get; }

    public string Alias { get; }

    /// <summary>
    /// Gets or sets the full name of the environment variable whose value overrides the content of the file, or
    /// <c>null</c> when the file has no such variable or when <see cref="EnvironmentVariableSuffix"/> is set.
    /// </summary>
    public string? EnvironmentVariableName { get; set; }

    /// <summary>
    /// Gets or sets the suffix of the environment variable whose value overrides the content of the file. The full
    /// name is obtained by prepending the environment variable prefix of the product profile, so the same
    /// configuration type serves every product family. The value is <c>null</c> when the file has no such variable.
    /// </summary>
    public string? EnvironmentVariableSuffix { get; set; }
}