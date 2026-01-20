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
        this.FileSystem.CreateDirectory( dotnetRootX64 );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( x64DotnetPath )! );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( defaultDotnetPath )! );
        this.FileSystem.WriteAllText( x64DotnetPath, string.Empty );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( defaultDotnetPath )! );
        this.FileSystem.WriteAllText( defaultDotnetPath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetPath )! );
        this.FileSystem.WriteAllText( dotnetPath, string.Empty );

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

        this.FileSystem.CreateDirectory( Path.GetDirectoryName( x64DotnetPath )! );
        this.FileSystem.WriteAllText( x64DotnetPath, string.Empty );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( defaultDotnetPath )! );
        this.FileSystem.WriteAllText( defaultDotnetPath, string.Empty );

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

        this.FileSystem.CreateDirectory( Path.GetDirectoryName( microsoftPath )! );
        this.FileSystem.WriteAllText( microsoftPath, string.Empty );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( ubuntuPath )! );
        this.FileSystem.WriteAllText( ubuntuPath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( ubuntuPath )! );
        this.FileSystem.WriteAllText( ubuntuPath, string.Empty );

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

        this.FileSystem.CreateDirectory( Path.GetDirectoryName( x64Path )! );
        this.FileSystem.WriteAllText( x64Path, string.Empty );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( defaultPath )! );
        this.FileSystem.WriteAllText( defaultPath, string.Empty );

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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
    }

    [Fact]
    public void ReSharperHost_DirectoriesExcluded_FromPATH()
    {
        // Arrange
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        const string riderPath = "C:\\Users\\test\\AppData\\Local\\JetBrains\\Installations\\ReSharperHost";
        const string validPath = "C:\\dotnet\\bin";
        var dotnetExePath = Path.Combine( validPath, "dotnet.exe" );

        this.EnvironmentVariableProvider.Environment["PATH"] = $"{riderPath};{validPath}";
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( dotnetExePath, result );
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
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( customDotnetPath )! );
        this.FileSystem.WriteAllText( customDotnetPath, string.Empty );
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( defaultDotnetPath )! );
        this.FileSystem.WriteAllText( defaultDotnetPath, string.Empty );

        // Act
        var result = this._platformInfo.DotNetExePath;

        // Assert
        Assert.Equal( customDotnetPath, result );
    }
}