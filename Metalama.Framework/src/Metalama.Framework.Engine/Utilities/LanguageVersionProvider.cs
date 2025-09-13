// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Utilities;

internal class LanguageVersionProvider : ILanguageVersionProvider
{
    private readonly IPlatformInfo _platformInfo;
    private readonly IProjectOptions _projectOptions;
    private LanguageVersion? _cachedValue;

    public LanguageVersionProvider( ProjectServiceProvider serviceProvider )
    {
        this._platformInfo = serviceProvider.Global.GetRequiredBackstageService<IPlatformInfo>();
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
        if ( this._platformInfo.DotNetSdkVersion == null )
        {
            throw new InvalidOperationException( "Unknown version of the .NET SDK." );
        }

        if ( !Version.TryParse( this._platformInfo.DotNetSdkVersion, out var version ) )
        {
            throw new AssertionFailedException( $"Cannot parse the .NET SDK version '{this._platformInfo.DotNetSdkVersion}'." );
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
}