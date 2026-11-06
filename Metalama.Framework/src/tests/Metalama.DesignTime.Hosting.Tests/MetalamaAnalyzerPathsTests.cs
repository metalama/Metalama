// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.DesignTime.Hosting.Tests;

/// <summary>
/// Tests <see cref="MetalamaAnalyzerPaths"/> against a global packages folder laid out the way NuGet lays it out.
/// </summary>
public sealed class MetalamaAnalyzerPathsTests : IDisposable
{
    private readonly string _packagesFolder = Path.Combine( Path.GetTempPath(), "Metalama.DesignTime.Hosting.Tests", Guid.NewGuid().ToString( "N" ) );

    [Theory]
    [InlineData( "2027.0.4-Preview" )]
    [InlineData( "[2027.0.4-Preview]" )]
    [InlineData( "[2027.0.4-Preview, 2028.0.0)" )]
    public void ReturnsTheAnalyzersOfMetalamaFrameworkAndMetalamaCompiler( string compilerVersionRange )
    {
        var frameworkDirectory = this.CreatePackage( "metalama.framework", "2027.0.5", "Metalama.Framework.CompilerExtensions.dll", "Metalama.Framework.Analyzers.dll" );

        File.WriteAllText(
            Path.Combine( frameworkDirectory, "metalama.framework.nuspec" ),
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
               <metadata>
                 <id>Metalama.Framework</id>
                 <dependencies>
                   <group targetFramework=".NETStandard2.0">
                     <dependency id="Metalama.Framework.Redist" version="2027.0.5" />
                     <dependency id="Metalama.Compiler" version="{compilerVersionRange}" exclude="Build,Analyzers" />
                   </group>
                 </dependencies>
               </metadata>
             </package>
             """ );

        this.CreatePackage( "metalama.compiler", "2027.0.4-preview", "Metalama.Compiler.Interface.dll" );

        var paths = MetalamaAnalyzerPaths.FromPackagesFolder( this._packagesFolder, "2027.0.5" );

        Assert.Equal(
            ["Metalama.Framework.Analyzers.dll", "Metalama.Framework.CompilerExtensions.dll", "Metalama.Compiler.Interface.dll"],
            paths.Select( Path.GetFileName ) );
    }

    [Fact]
    public void ThrowsWhenTheCompilerPackageIsMissing()
    {
        var frameworkDirectory = this.CreatePackage( "metalama.framework", "2027.0.5", "Metalama.Framework.CompilerExtensions.dll" );

        File.WriteAllText(
            Path.Combine( frameworkDirectory, "metalama.framework.nuspec" ),
            """
            <package><metadata><dependencies><dependency id="Metalama.Compiler" version="2027.0.4" /></dependencies></metadata></package>
            """ );

        Assert.Throws<DirectoryNotFoundException>( () => MetalamaAnalyzerPaths.FromPackageDirectory( frameworkDirectory ) );
    }

    private string CreatePackage( string id, string version, params string[] analyzers )
    {
        var packageDirectory = Path.Combine( this._packagesFolder, id, version );
        var analyzerDirectory = Path.Combine( packageDirectory, "analyzers", "dotnet", "cs" );
        Directory.CreateDirectory( analyzerDirectory );

        foreach ( var analyzer in analyzers )
        {
            File.WriteAllText( Path.Combine( analyzerDirectory, analyzer ), "" );
        }

        return packageDirectory;
    }

    public void Dispose()
    {
        if ( Directory.Exists( this._packagesFolder ) )
        {
            Directory.Delete( this._packagesFolder, true );
        }
    }
}
