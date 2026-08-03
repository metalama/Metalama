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

/// <summary>
/// Tests the selection of the <c>dotnet</c> executable when 64-bit Windows carries both a 32-bit and a 64-bit .NET
/// installation.
/// </summary>
/// <remarks>
/// A 32-bit host, such as the <c>MSBuild.exe</c> of Visual Studio or a 32-bit compiler server, expands
/// <c>%ProgramFiles%</c> to <c>C:\Program Files (x86)</c>, so the 32-bit installation is found first even though the
/// project was built with the 64-bit .NET SDK. The nested reference-assembly build then runs against a set of .NET SDKs
/// that does not contain the version that the outer build used. See issue #1745.
/// </remarks>
public sealed class PlatformInfoArchitectureTests : TestsBase
{
    private const string _programFilesX86 = "C:\\Program Files (x86)";
    private const string _programFilesX64 = "C:\\Program Files";

    public PlatformInfoArchitectureTests( ITestOutputHelper logger ) : base( logger ) { }

    private void CreateDotNetWithSdk( string dotnetExePath, params string[] sdkVersions )
    {
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( dotnetExePath )! );
        this.FileSystem.WriteAllText( dotnetExePath, string.Empty );

        var sdkDirectory = Path.Combine( Path.GetDirectoryName( dotnetExePath )!, "sdk" );
        this.FileSystem.CreateDirectory( sdkDirectory );

        foreach ( var sdkVersion in sdkVersions )
        {
            this.FileSystem.CreateDirectory( Path.Combine( sdkDirectory, sdkVersion ) );
        }
    }

    /// <summary>
    /// Configures a 32-bit process on 64-bit Windows, which reads <c>C:\Program Files (x86)</c> from
    /// <c>%ProgramFiles%</c> and <c>C:\Program Files</c> from <c>%ProgramW6432%</c>.
    /// </summary>
    private void ArrangeX86ProcessOnX64Windows()
    {
        this.RuntimeInformation.Platform = OSPlatform.Windows;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X86;
        this.RuntimeInformation.TestOSArchitecture = Architecture.X64;

        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = _programFilesX86;
        this.EnvironmentVariableProvider.Environment["ProgramW6432"] = _programFilesX64;
    }

    /// <summary>
    /// The defect of issue #1745: a 32-bit process must not prefer the 32-bit installation, because the project was
    /// built by the .NET SDK of the native architecture and the nested build is pinned to that version.
    /// </summary>
    [Fact]
    public void X86Process_PrefersTheNativeInstallation()
    {
        this.ArrangeX86ProcessOnX64Windows();

        var x86Dotnet = Path.Combine( _programFilesX86, "dotnet", "dotnet.exe" );
        var x64Dotnet = Path.Combine( _programFilesX64, "dotnet", "dotnet.exe" );

        // The 32-bit installation carries an old .NET SDK, the 64-bit one carries the version that the outer build used.
        this.CreateDotNetWithSdk( x86Dotnet, "8.0.423" );
        this.CreateDotNetWithSdk( x64Dotnet, "8.0.423", "10.0.302" );

        var result = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>().DotNetExePath;

        Assert.Equal( x64Dotnet, result );
    }

    /// <summary>
    /// The 32-bit installation must still be used when it is the only one, so that a machine that carries no native
    /// .NET installation keeps working.
    /// </summary>
    [Fact]
    public void X86Process_FallsBackToTheX86Installation_WhenThereIsNoNativeOne()
    {
        this.ArrangeX86ProcessOnX64Windows();

        var x86Dotnet = Path.Combine( _programFilesX86, "dotnet", "dotnet.exe" );
        this.CreateDotNetWithSdk( x86Dotnet, "8.0.423" );

        var result = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>().DotNetExePath;

        Assert.Equal( x86Dotnet, result );
    }

    /// <summary>
    /// The native installation must be skipped when it has no .NET SDK, exactly as any other candidate is.
    /// </summary>
    [Fact]
    public void X86Process_SkipsTheNativeInstallation_WhenItHasNoSdk()
    {
        this.ArrangeX86ProcessOnX64Windows();

        var x86Dotnet = Path.Combine( _programFilesX86, "dotnet", "dotnet.exe" );
        var x64Dotnet = Path.Combine( _programFilesX64, "dotnet", "dotnet.exe" );

        this.CreateDotNetWithSdk( x86Dotnet, "8.0.423" );

        // A runtime-only native installation: the executable exists but there is no sdk directory beside it.
        this.FileSystem.CreateDirectory( Path.GetDirectoryName( x64Dotnet )! );
        this.FileSystem.WriteAllText( x64Dotnet, string.Empty );

        var result = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>().DotNetExePath;

        Assert.Equal( x86Dotnet, result );
    }

    /// <summary>
    /// In a 64-bit process the two variables have the same value, so the resolution must be unchanged.
    /// </summary>
    [Fact]
    public void X64Process_IsUnaffected()
    {
        this.RuntimeInformation.Platform = OSPlatform.Windows;
        this.RuntimeInformation.TestProcessArchitecture = Architecture.X64;
        this.RuntimeInformation.TestOSArchitecture = Architecture.X64;

        this.EnvironmentVariableProvider.Environment["ProgramFiles"] = _programFilesX64;
        this.EnvironmentVariableProvider.Environment["ProgramW6432"] = _programFilesX64;

        var x64Dotnet = Path.Combine( _programFilesX64, "dotnet", "dotnet.exe" );
        this.CreateDotNetWithSdk( x64Dotnet, "10.0.302" );

        var result = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>().DotNetExePath;

        Assert.Equal( x64Dotnet, result );
    }
}
