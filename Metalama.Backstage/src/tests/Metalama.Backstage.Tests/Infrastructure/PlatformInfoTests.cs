// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

public sealed class PlatformInfoTests : TestsBase
{
    private readonly IPlatformInfo _platformInfo;

    public PlatformInfoTests( ITestOutputHelper logger ) : base( logger )
    {
        this._platformInfo = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>();
    }

    /// <summary>
    /// Creates a dotnet.exe file at the given path with an SDK directory, so it passes the SDK check.
    /// </summary>
    private void CreateDotNetWithSdk( string dotnetExePath )
    {
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );
        this.FileSystem.CreateDirectory( Path.Combine( Path.GetDirectoryName( dotnetExePath )!, "sdk" ) );
    }

    /// <summary>
    /// Creates a dotnet.exe file at the given path without an SDK directory (runtime-only installation).
    /// </summary>
    private void CreateDotNetWithoutSdk( string dotnetExePath )
    {
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );
    }

    [Fact]
    public void DOTNET_HOST_PATH_HasHighestPrecedence()
    {
        // Arrange
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string dotnetHostPath = "C:\\host\\dotnet.exe";
        const string dotnetRootX64 = "C:\\custom\\dotnet64";
        const string dotnetRoot = "C:\\custom\\dotnet";

        this.EnvironmentVariableProvider.Environment["DOTNET_HOST_PATH"] = dotnetHostPath;
        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT_X64"] = dotnetRootX64;
        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = dotnetRoot;
        this.CreateDotNetWithSdk( dotnetHostPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetHostPath, result );
    }

    [Fact]
    public void DOTNET_HOST_PATH_Ignored_WhenNoSdkDirectory()
    {
        // Arrange: Visual Studio sets DOTNET_HOST_PATH to its bundled runtime-only dotnet.exe,
        // which has no SDK. We should skip it and fall back to the default location.
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string vsRuntimeDotnet = "C:\\Program Files\\Microsoft Visual Studio\\18\\Professional\\dotnet\\net8.0\\runtime\\dotnet.exe";
        const string programFiles = "C:\\Program Files";
        var defaultDotnetPath = Path.Combine( programFiles, "dotnet", "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_HOST_PATH"] = vsRuntimeDotnet;
        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = programFiles;

        // Create the VS-bundled dotnet.exe (no sdk directory).
        this.CreateDotNetWithoutSdk( vsRuntimeDotnet );

        // Create the system dotnet.exe (with SDK).
        this.CreateDotNetWithSdk( defaultDotnetPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert: Should skip VS-bundled dotnet and use system installation.
        Assert.Equal( defaultDotnetPath, result );
    }

    [Fact]
    public void DOTNET_ROOT_Ignored_WhenNoSdkDirectory()
    {
        // Arrange: Rider sets DOTNET_ROOT to its own limited dotnet installation without an SDK.
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string riderDotnetRoot = "C:\\RiderDotNet";
        const string programFiles = "C:\\Program Files";
        var riderDotnetPath = Path.Combine( riderDotnetRoot, "dotnet.exe" );
        var defaultDotnetPath = Path.Combine( programFiles, "dotnet", "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = riderDotnetRoot;
        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = programFiles;

        // Rider's dotnet has no SDK.
        this.CreateDotNetWithoutSdk( riderDotnetPath );

        // System dotnet has SDK.
        this.CreateDotNetWithSdk( defaultDotnetPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert: Should skip Rider's DOTNET_ROOT and use system installation.
        Assert.Equal( defaultDotnetPath, result );
    }

    [Fact]
    public void DOTNET_ROOT_X64_HasPrecedence_OnX64Process()
    {
        // Arrange
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string dotnetRootX64 = "C:\\custom\\dotnet64";
        const string dotnetRoot = "C:\\custom\\dotnet";
        var dotnetExePath = Path.Combine( dotnetRootX64, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT_X64"] = dotnetRootX64;
        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = dotnetRoot;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void DOTNET_ROOT_X86_HasPrecedence_OnX86Process()
    {
        // Arrange
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X86;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string dotnetRootX86 = "C:\\custom\\dotnet86";
        const string dotnetRoot = "C:\\custom\\dotnet";
        var dotnetExePath = Path.Combine( dotnetRootX86, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT_X86"] = dotnetRootX86;
        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = dotnetRoot;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void DOTNET_ROOT_Parentheses_X86_FallsBack_OnX86Windows()
    {
        // Arrange
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X86;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string dotnetRootX86Paren = "C:\\custom\\dotnet86paren";
        var dotnetExePath = Path.Combine( dotnetRootX86Paren, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT(x86)"] = dotnetRootX86Paren;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void DOTNET_ROOT_ARM64_HasPrecedence_OnARM64Process()
    {
        // Arrange
        this.RuntimeInformation.TestProcessArchitecture = Architecture.Arm64;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string dotnetRootArm64 = "C:\\custom\\dotnetarm64";
        const string dotnetRoot = "C:\\custom\\dotnet";
        var dotnetExePath = Path.Combine( dotnetRootArm64, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT_ARM64"] = dotnetRootArm64;
        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = dotnetRoot;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void DOTNET_ROOT_FallsBack_WhenArchSpecificNotSet()
    {
        // Arrange
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string dotnetRoot = "C:\\fallback\\dotnet";
        var dotnetExePath = Path.Combine( dotnetRoot, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = dotnetRoot;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void Windows_DefaultLocation_UsedWhenNoDOTNET_ROOT()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;

        const string programFiles = "C:\\Program Files";
        var dotnetExePath = Path.Combine( programFiles, "dotnet", "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = programFiles;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void Windows_ARM64_X64Subfolder_CheckedFirst()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.TestOSArchitecture = Architecture.Arm64;

        const string programFiles = "C:\\Program Files";
        var x64DotnetPath = Path.Combine( programFiles, "dotnet", "x64", "dotnet.exe" );
        var defaultDotnetPath = Path.Combine( programFiles, "dotnet", "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = programFiles;
        this.CreateDotNetWithSdk( x64DotnetPath );
        this.CreateDotNetWithSdk( defaultDotnetPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( x64DotnetPath, result );
    }

    [Fact]
    public void MacOS_DefaultLocation_Used()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.OSX;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;

        var dotnetPath = Path.Combine( "/usr/local/share/dotnet", "dotnet" );
        this.CreateDotNetWithSdk( dotnetPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetPath, result );
    }

    [Fact]
    public void MacOS_ARM64_X64Subfolder_CheckedFirst()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.OSX;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.TestOSArchitecture = Architecture.Arm64;

        var x64DotnetPath = Path.Combine( "/usr/local/share/dotnet", "x64", "dotnet" );
        var defaultDotnetPath = Path.Combine( "/usr/local/share/dotnet", "dotnet" );

        this.CreateDotNetWithSdk( x64DotnetPath );
        this.CreateDotNetWithSdk( defaultDotnetPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( x64DotnetPath, result );
    }

    [Fact]
    public void Linux_MicrosoftPackages_LocationCheckedFirst()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Linux;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;

        var microsoftPath = Path.Combine( "/usr/share/dotnet", "dotnet" );
        var ubuntuPath = Path.Combine( "/usr/lib/dotnet", "dotnet" );

        this.CreateDotNetWithSdk( microsoftPath );
        this.CreateDotNetWithSdk( ubuntuPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( microsoftPath, result );
    }

    [Fact]
    public void Linux_UbuntuJammy_LocationUsedAsFallback()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Linux;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;

        var ubuntuPath = Path.Combine( "/usr/lib/dotnet", "dotnet" );
        this.CreateDotNetWithSdk( ubuntuPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( ubuntuPath, result );
    }

    [Fact]
    public void Linux_ARM64_X64Subfolder_CheckedFirst()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Linux;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.TestOSArchitecture = Architecture.Arm64;

        var x64Path = Path.Combine( "/usr/share/dotnet", "x64", "dotnet" );
        var defaultPath = Path.Combine( "/usr/share/dotnet", "dotnet" );

        this.CreateDotNetWithSdk( x64Path );
        this.CreateDotNetWithSdk( defaultPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( x64Path, result );
    }

    [Fact]
    public void PATH_Variable_UsedWhenNoDOTNET_ROOTOrDefaultLocation()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string customPath = "C:\\custom\\bin";
        var dotnetExePath = Path.Combine( customPath, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["PATH"] = customPath;
        this.CreateDotNetWithSdk( dotnetExePath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void PATH_SkipsEntriesWithoutSdk()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string noSdkPath = "C:\\nosdk\\bin";
        const string validPath = "C:\\dotnet\\bin";
        var noSdkDotnet = Path.Combine( noSdkPath, "dotnet.exe" );
        var validDotnet = Path.Combine( validPath, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["PATH"] = $"{noSdkPath};{validPath}";
        this.CreateDotNetWithoutSdk( noSdkDotnet );
        this.CreateDotNetWithSdk( validDotnet );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( validDotnet, result );
    }

    [Fact]
    public void FallsBackTo_DotnetCommand_WhenNotFound()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( "dotnet", result );
    }

    [Fact]
    public void DOTNET_ROOT_PrecedesDefaultLocations()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;

        const string dotnetRoot = "C:\\custom\\dotnet";
        const string programFiles = "C:\\Program Files";
        var customDotnetPath = Path.Combine( dotnetRoot, "dotnet.exe" );
        var defaultDotnetPath = Path.Combine( programFiles, "dotnet", "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["DOTNET_ROOT"] = dotnetRoot;
        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = programFiles;
        this.CreateDotNetWithSdk( customDotnetPath );
        this.CreateDotNetWithSdk( defaultDotnetPath );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( customDotnetPath, result );
    }
}