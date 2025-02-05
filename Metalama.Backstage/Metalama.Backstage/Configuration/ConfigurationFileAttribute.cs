// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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