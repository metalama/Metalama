// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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

    public string? EnvironmentVariableName { get; set; }
}