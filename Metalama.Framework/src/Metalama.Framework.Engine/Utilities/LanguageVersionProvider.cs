// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities;

internal sealed class LanguageVersionProvider : ILanguageVersionProvider
{
    private readonly IProjectOptions _projectOptions;
    private LanguageVersion? _cachedValue;

    public LanguageVersionProvider( ProjectServiceProvider serviceProvider )
    {
        this._projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();
    }

    /// <summary>
    /// Gets the highest C# language version supported when compiling the template,
    /// which depends on the SDK and not on the Roslyn version of the current process.
    /// </summary>
    /// <returns></returns>
    public LanguageVersion GetCompileTimeLanguageVersion() => this._cachedValue ??= this.GetCompileTimeLanguageVersionCore();

    private LanguageVersion GetCompileTimeLanguageVersionCore()
    {
        // NETCoreSdkVersion can be null (property not defined) or empty (old-style .NET Framework projects
        // built with msbuild.exe where Microsoft.NETCoreSdk.BundledVersions.props is not imported).
        if ( string.IsNullOrEmpty( this._projectOptions.SdkVersion ) )
        {
            // When building with msbuild.exe from Visual Studio (no .NET SDK), we use the project's
            // language version constrained by what the MSBuild's bundled Roslyn version supports.
            return this.GetLanguageVersionFromMSBuild();
        }

        return this.GetLanguageVersionFromDotNetSdk();
    }

    private LanguageVersion GetLanguageVersionFromDotNetSdk()
    {
        var mainVersion = this._projectOptions.SdkVersion!.Split( '-' )[0];

        if ( !Version.TryParse( mainVersion, out var version ) )
        {
            throw new AssertionFailedException( $"Cannot parse the .NET SDK version '{this._projectOptions.SdkVersion}'." );
        }

        var sdkSupportedVersion = version.Major switch
        {
#if ROSLYN_5_0_0_OR_GREATER
            >= 10 => LanguageVersion.CSharp14,
#endif
#if ROSLYN_4_12_0_OR_GREATER
            >= 9 => LanguageVersion.CSharp13,
#endif
            >= 8 => LanguageVersion.CSharp12,
            _ => throw new PlatformNotSupportedException( $"Unsupported .NET SDK version: {version}." )
        };

        var projectVersion = this._projectOptions.LanguageVersion;

        if ( sdkSupportedVersion >= projectVersion )
        {
            return projectVersion;
        }
        else
        {
            return sdkSupportedVersion;
        }
    }

    private LanguageVersion GetLanguageVersionFromMSBuild()
    {
        var msBuildBinPath = this._projectOptions.MSBuildBinPath;

        if ( string.IsNullOrEmpty( msBuildBinPath ) )
        {
            throw new InvalidOperationException(
                $"Cannot determine the compile-time language version: neither {nameof(MSBuildPropertyNames.NETCoreSdkVersion)} nor " +
                $"{nameof(MSBuildPropertyNames.MSBuildBinPath)} is defined for project '{this._projectOptions.ProjectName}'." );
        }

        // Find Roslyn assembly in the MSBuild bin path to determine the version.
        // The Roslyn folder can be in the bin path itself (e.g., ...\Bin\Roslyn) or in the parent directory
        // when using the amd64 subfolder (e.g., ...\Bin\amd64 -> ...\Bin\Roslyn).
        var roslynDllPath = Path.Combine( msBuildBinPath, "Roslyn", "Microsoft.CodeAnalysis.CSharp.dll" );

        if ( !File.Exists( roslynDllPath ) )
        {
            // Try parent directory (for amd64 subfolder case).
            var parentPath = Path.GetDirectoryName( msBuildBinPath );

            if ( parentPath != null )
            {
                roslynDllPath = Path.Combine( parentPath, "Roslyn", "Microsoft.CodeAnalysis.CSharp.dll" );
            }
        }

        if ( !File.Exists( roslynDllPath ) )
        {
            throw new InvalidOperationException(
                $"Cannot determine the compile-time language version: could not find Roslyn in '{msBuildBinPath}' or its parent directory." );
        }

        var roslynVersion = AssemblyName.GetAssemblyName( roslynDllPath ).Version;

        if ( roslynVersion == null )
        {
            throw new InvalidOperationException(
                $"Cannot determine the compile-time language version: could not read assembly version from '{roslynDllPath}'." );
        }

        var msBuildSupportedVersion = SupportedCSharpVersions.GetMaxLanguageVersion( roslynVersion );

        var projectVersion = this._projectOptions.LanguageVersion;

        if ( msBuildSupportedVersion >= projectVersion )
        {
            return projectVersion;
        }
        else
        {
            return msBuildSupportedVersion;
        }
    }
}