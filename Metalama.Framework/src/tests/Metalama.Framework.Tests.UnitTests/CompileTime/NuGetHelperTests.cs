// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public sealed class NuGetHelperTests : UnitTestClass
{
    [Fact]
    public void RelativeFallbackPackageFoldersAreResolvedToAbsolutePaths()
    {
        using var testContext = this.CreateTestContext();

        // Create a nuget.config with a relative path in fallbackPackageFolders, as in issue #1414.
        const string content = """
                               <configuration>
                                   <packageSources>
                                       <clear />
                                       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                   </packageSources>
                                   <fallbackPackageFolders>
                                       <add key="SomeFallback" value="nuget/fallback" />
                                   </fallbackPackageFolders>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        // The relative path "nuget/fallback" should be resolved to an absolute path
        // relative to the directory containing the nuget.config file.
        var fallbackElement = mergedConfig.Root.AssertNotNull()
            .Element( "fallbackPackageFolders" )
            .AssertNotNull()
            .Element( "add" )
            .AssertNotNull();

        var value = fallbackElement.Attribute( "value" ).AssertNotNull().Value;
        var expectedAbsolutePath = Path.Combine( testContext.BaseDirectory, "nuget", "fallback" );

        Assert.Equal( expectedAbsolutePath, value );
    }

    [Fact]
    public void RelativePackageSourcePathsAreResolvedToAbsolutePaths()
    {
        using var testContext = this.CreateTestContext();

        // A nuget.config with a relative local package source path.
        const string content = """
                               <configuration>
                                   <packageSources>
                                       <clear />
                                       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                       <add key="LocalPackages" value="packages/local" />
                                   </packageSources>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var localSourceElement = mergedConfig.Root.AssertNotNull()
            .Element( "packageSources" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "LocalPackages" );

        var value = localSourceElement.Attribute( "value" ).AssertNotNull().Value;
        var expectedAbsolutePath = Path.Combine( testContext.BaseDirectory, "packages", "local" );

        Assert.Equal( expectedAbsolutePath, value );
    }

    [Fact]
    public void AbsolutePathsAreNotModified()
    {
        using var testContext = this.CreateTestContext();

        var absolutePath = Path.Combine( testContext.BaseDirectory, "abs", "fallback" );

        var content = $"""
                       <configuration>
                           <packageSources>
                               <clear />
                               <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                           </packageSources>
                           <fallbackPackageFolders>
                               <add key="SomeFallback" value="{absolutePath}" />
                           </fallbackPackageFolders>
                       </configuration>
                       """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var fallbackElement = mergedConfig.Root.AssertNotNull()
            .Element( "fallbackPackageFolders" )
            .AssertNotNull()
            .Element( "add" )
            .AssertNotNull();

        var value = fallbackElement.Attribute( "value" ).AssertNotNull().Value;

        Assert.Equal( absolutePath, value );
    }

    [Fact]
    public void HttpUrlsAreNotModified()
    {
        using var testContext = this.CreateTestContext();

        const string content = """
                               <configuration>
                                   <packageSources>
                                       <clear />
                                       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                   </packageSources>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var nugetOrgElement = mergedConfig.Root.AssertNotNull()
            .Element( "packageSources" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "nuget.org" );

        var value = nugetOrgElement.Attribute( "value" ).AssertNotNull().Value;

        Assert.Equal( "https://api.nuget.org/v3/index.json", value );
    }

    [Fact]
    public void FileUrlsAreNotModified()
    {
        using var testContext = this.CreateTestContext();

        const string content = """
                               <configuration>
                                   <packageSources>
                                       <clear />
                                       <add key="local" value="file://C:/local-packages" />
                                   </packageSources>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var localElement = mergedConfig.Root.AssertNotNull()
            .Element( "packageSources" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "local" );

        var value = localElement.Attribute( "value" ).AssertNotNull().Value;

        Assert.Equal( "file://C:/local-packages", value );
    }

    [Fact]
    public void EnvironmentVariablePathsAreNotResolved()
    {
        using var testContext = this.CreateTestContext();

        const string content = """
                               <configuration>
                                   <packageSources>
                                       <clear />
                                       <add key="envSource" value="%MY_NUGET_SOURCE_UNDEFINED_VAR%" />
                                   </packageSources>
                                   <fallbackPackageFolders>
                                       <add key="envFallback" value="%MY_NUGET_FALLBACK_UNDEFINED_VAR%" />
                                   </fallbackPackageFolders>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var envSourceElement = mergedConfig.Root.AssertNotNull()
            .Element( "packageSources" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "envSource" );

        var envFallbackElement = mergedConfig.Root.AssertNotNull()
            .Element( "fallbackPackageFolders" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "envFallback" );

        // Undefined environment variables should not be modified — NuGet handles expansion at runtime.
        Assert.Equal( "%MY_NUGET_SOURCE_UNDEFINED_VAR%", envSourceElement.Attribute( "value" ).AssertNotNull().Value );
        Assert.Equal( "%MY_NUGET_FALLBACK_UNDEFINED_VAR%", envFallbackElement.Attribute( "value" ).AssertNotNull().Value );
    }

    [Fact]
    public void ConfigRepositoryPathRelativePathIsResolvedToAbsolute()
    {
        using var testContext = this.CreateTestContext();

        const string content = """
                               <configuration>
                                   <config>
                                       <add key="repositoryPath" value="packages/repo" />
                                   </config>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var repoPathElement = mergedConfig.Root.AssertNotNull()
            .Element( "config" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "repositoryPath" );

        var value = repoPathElement.Attribute( "value" ).AssertNotNull().Value;
        var expectedAbsolutePath = Path.Combine( testContext.BaseDirectory, "packages", "repo" );

        Assert.Equal( expectedAbsolutePath, value );
    }

    [Fact]
    public void ConfigGlobalPackagesFolderRelativePathIsResolvedToAbsolute()
    {
        using var testContext = this.CreateTestContext();

        const string content = """
                               <configuration>
                                   <config>
                                       <add key="globalPackagesFolder" value="my-packages" />
                                   </config>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var element = mergedConfig.Root.AssertNotNull()
            .Element( "config" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "globalPackagesFolder" );

        var value = element.Attribute( "value" ).AssertNotNull().Value;
        var expectedAbsolutePath = Path.Combine( testContext.BaseDirectory, "my-packages" );

        Assert.Equal( expectedAbsolutePath, value );
    }

    [Fact]
    public void ConfigNonPathKeysAreNotModified()
    {
        using var testContext = this.CreateTestContext();

        const string content = """
                               <configuration>
                                   <config>
                                       <add key="defaultPushSource" value="https://MyRepo/ES/api/v2/package" />
                                       <add key="http_proxy" value="host" />
                                   </config>
                               </configuration>
                               """;

        var configPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( configPath, content );

        var mergedConfig = MergeConfigFiles( testContext, configPath );

        var pushSourceElement = mergedConfig.Root.AssertNotNull()
            .Element( "config" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "defaultPushSource" );

        var proxyElement = mergedConfig.Root.AssertNotNull()
            .Element( "config" )
            .AssertNotNull()
            .Elements( "add" )
            .First( e => e.Attribute( "key" )?.Value == "http_proxy" );

        Assert.Equal( "https://MyRepo/ES/api/v2/package", pushSourceElement.Attribute( "value" ).AssertNotNull().Value );
        Assert.Equal( "host", proxyElement.Attribute( "value" ).AssertNotNull().Value );
    }

    [Fact]
    public void PackageSourceMappingDuplicateKeysAreMerged()
    {
        // Regression test for issue #1560: when parent and child nuget.config files both define
        // <packageSourceMapping> entries with the same key (without <clear/>), the merge should
        // replace the parent's entry with the child's, not create duplicates.
        using var testContext = this.CreateTestContext();

        const string parentConfig = """
                                    <configuration>
                                        <packageSources>
                                            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                            <add key="MyFeed" value="https://myfeed.example.com/index.json" />
                                        </packageSources>
                                        <packageSourceMapping>
                                            <packageSource key="nuget.org">
                                                <package pattern="*" />
                                            </packageSource>
                                            <packageSource key="MyFeed">
                                                <package pattern="MyCompany.*" />
                                            </packageSource>
                                        </packageSourceMapping>
                                    </configuration>
                                    """;

        const string childConfig = """
                                   <configuration>
                                       <packageSources>
                                           <add key="LocalFeed" value="https://localfeed.example.com/index.json" />
                                       </packageSources>
                                       <packageSourceMapping>
                                           <packageSource key="nuget.org">
                                               <package pattern="Newtonsoft.*" />
                                               <package pattern="System.*" />
                                           </packageSource>
                                           <packageSource key="LocalFeed">
                                               <package pattern="Local.*" />
                                           </packageSource>
                                       </packageSourceMapping>
                                   </configuration>
                                   """;

        var parentPath = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( parentPath, parentConfig );
        var subdir = Path.Combine( testContext.BaseDirectory, "sub" );
        Directory.CreateDirectory( subdir );
        var childPath = Path.Combine( subdir, "nuget.config" );
        File.WriteAllText( childPath, childConfig );

        var mergedConfig = MergeConfigFiles( testContext, childPath );

        // There should be exactly 3 packageSource entries: nuget.org (from child, replacing parent),
        // MyFeed (from parent), and LocalFeed (from child).
        var packageSourceMappingElement = mergedConfig.Root.AssertNotNull()
            .Element( "packageSourceMapping" )
            .AssertNotNull();

        var packageSourceElements = packageSourceMappingElement.Elements( "packageSource" ).ToList();

        Assert.Equal( 3, packageSourceElements.Count );

        // nuget.org should have the child's patterns (Newtonsoft.*, System.*), not the parent's (*).
        var nugetOrgElement = packageSourceElements.First( e => e.Attribute( "key" )?.Value == "nuget.org" );
        var nugetOrgPatterns = nugetOrgElement.Elements( "package" ).Select( e => e.Attribute( "pattern" )?.Value ).ToList();

        Assert.Equal( 2, nugetOrgPatterns.Count );
        Assert.Contains( "Newtonsoft.*", nugetOrgPatterns );
        Assert.Contains( "System.*", nugetOrgPatterns );

        // MyFeed should still have the parent's pattern.
        var myFeedElement = packageSourceElements.First( e => e.Attribute( "key" )?.Value == "MyFeed" );
        var myFeedPatterns = myFeedElement.Elements( "package" ).Select( e => e.Attribute( "pattern" )?.Value ).ToList();

        Assert.Single( myFeedPatterns );
        Assert.Equal( "MyCompany.*", myFeedPatterns[0] );

        // LocalFeed should have the child's pattern.
        var localFeedElement = packageSourceElements.First( e => e.Attribute( "key" )?.Value == "LocalFeed" );
        var localFeedPatterns = localFeedElement.Elements( "package" ).Select( e => e.Attribute( "pattern" )?.Value ).ToList();

        Assert.Single( localFeedPatterns );
        Assert.Equal( "Local.*", localFeedPatterns[0] );
    }

    [Fact]
    public void MergeTest()
    {
        using var testContext = this.CreateTestContext();

        const string content1 = """
                                <configuration>
                                    <config>
                                        <add key="repositoryPath" value="%PACKAGEHOME%/External" />
                                    </config>

                                    <packageSources>
                                        <add key="NuGet official package source" value="https://api.nuget.org/v3/index.json" />
                                    </packageSources>

                                    <packageSourceCredentials />

                                    <disabledPackageSources />

                                    <apikeys>
                                        <add key="https://MyRepo/ES/api/v2/package" value="encrypted_api_key" />
                                    </apikeys>

                                    <trustedSigners>
                                        <author name="microsoft">
                                            <certificate fingerprint="3F9001EA83C560D712C24CF213C3D312CB3BFF51EE89435D3430BD06B5D0EECE" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                                            <certificate fingerprint="AA12DA22A49BCE7D5C1AE64CC1F3D892F150DA76140F210ABD2CBFFCA2C18A27" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                                            <certificate fingerprint="566A31882BE208BE4422F7CFD66ED09F5D4524A5994F50CCC8B05EC0528C1353" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                                        </author>
                                    </trustedSigners>
                                </configuration>
                                """;

        const string content2 = """
                                <configuration>
                                    <config>
                                        <add key="defaultPushSource" value="https://MyRepo/ES/api/v2/package" />
                                        <add key="http_proxy" value="host" />
                                        <add key="http_proxy.user" value="username" />
                                        <add key="http_proxy.password" value="encrypted_password" />
                                    </config>

                                    <packageRestore>
                                        <add key="enabled" value="True" />
                                        <add key="automatic" value="True" />
                                    </packageRestore>

                                    <packageSources>
                                        <clear />
                                        <add key="MyRepo - ES" value="https://MyRepo/ES/nuget" />
                                    </packageSources>

                                    <packageSourceCredentials />

                                    <disabledPackageSources />

                                    <trustedSigners>
                                        <repository name="nuget.org" serviceIndex="https://api.nuget.org/v3/index.json">
                                             <certificate fingerprint="0E5F38F57DC1BCC806D8494F4F90FBCEDD988B46760709CBEEC6F4219AA6157D" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                                             <certificate fingerprint="5A2901D6ADA3D18260B9C6DFE2133C95D74B9EEF6AE0E5DC334C8454D1477DF4" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                                             <certificate fingerprint="1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                                             <owners>microsoft;aspnet;nuget</owners>
                                         </repository>
                                    </trustedSigners>
                                </configuration>
                                """;

        var path1 = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path1, content1 );
        var subdir = Path.Combine( testContext.BaseDirectory, "sub" );
        Directory.CreateDirectory( subdir );
        var path2 = Path.Combine( subdir, "nuget.config" );
        File.WriteAllText( path2, content2 );

        var mergedConfig = MergeConfigFiles( testContext, path2 ).ToString();

        const string expectedMergedConfig =
            """
            <configuration>
              <config>
                <add key="repositoryPath" value="%PACKAGEHOME%/External" />
                <add key="defaultPushSource" value="https://MyRepo/ES/api/v2/package" />
                <add key="http_proxy" value="host" />
                <add key="http_proxy.user" value="username" />
                <add key="http_proxy.password" value="encrypted_password" />
              </config>
              <packageSources>
                <clear />
                <add key="MyRepo - ES" value="https://MyRepo/ES/nuget" />
              </packageSources>
              <packageSourceCredentials />
              <disabledPackageSources />
              <apikeys>
                <add key="https://MyRepo/ES/api/v2/package" value="encrypted_api_key" />
              </apikeys>
              <trustedSigners>
                <author name="microsoft">
                  <certificate fingerprint="3F9001EA83C560D712C24CF213C3D312CB3BFF51EE89435D3430BD06B5D0EECE" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                  <certificate fingerprint="AA12DA22A49BCE7D5C1AE64CC1F3D892F150DA76140F210ABD2CBFFCA2C18A27" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                  <certificate fingerprint="566A31882BE208BE4422F7CFD66ED09F5D4524A5994F50CCC8B05EC0528C1353" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                </author>
                <repository name="nuget.org" serviceIndex="https://api.nuget.org/v3/index.json">
                     <certificate fingerprint="0E5F38F57DC1BCC806D8494F4F90FBCEDD988B46760709CBEEC6F4219AA6157D" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                     <certificate fingerprint="5A2901D6ADA3D18260B9C6DFE2133C95D74B9EEF6AE0E5DC334C8454D1477DF4" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                     <certificate fingerprint="1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
                     <owners>microsoft;aspnet;nuget</owners>
                 </repository>
              </trustedSigners>
              <packageRestore>
                <add key="enabled" value="True" />
                <add key="automatic" value="True" />
              </packageRestore>
            </configuration>
            """;

        AssertEx.WhitespaceInvariantEqual( expectedMergedConfig, mergedConfig );
    }

    [Fact]
    public void PackageSourceMappingClearRemovesAllInheritedMappings()
    {
        using var testContext = this.CreateTestContext();

        const string parentConfig = """
                                    <configuration>
                                        <packageSources>
                                            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                            <add key="MyFeed" value="https://myfeed/nuget" />
                                        </packageSources>
                                        <packageSourceMapping>
                                            <packageSource key="nuget.org">
                                                <package pattern="*" />
                                            </packageSource>
                                            <packageSource key="MyFeed">
                                                <package pattern="MyCompany.*" />
                                                <package pattern="MyCompany.Tools.*" />
                                            </packageSource>
                                        </packageSourceMapping>
                                    </configuration>
                                    """;

        const string childConfig = """
                                   <configuration>
                                       <packageSources>
                                           <clear />
                                           <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                           <add key="PrivateFeed" value="https://privatefeed/nuget" />
                                       </packageSources>
                                       <packageSourceMapping>
                                           <clear />
                                           <packageSource key="nuget.org">
                                               <package pattern="*" />
                                           </packageSource>
                                           <packageSource key="PrivateFeed">
                                               <package pattern="Internal.*" />
                                           </packageSource>
                                       </packageSourceMapping>
                                   </configuration>
                                   """;

        var path1 = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path1, parentConfig );
        var subdir = Path.Combine( testContext.BaseDirectory, "sub" );
        Directory.CreateDirectory( subdir );
        var path2 = Path.Combine( subdir, "nuget.config" );
        File.WriteAllText( path2, childConfig );

        var mergedConfig = MergeConfigFiles( testContext, path2 ).ToString();

        const string expectedMergedConfig =
            """
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="PrivateFeed" value="https://privatefeed/nuget" />
              </packageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
                <packageSource key="PrivateFeed">
                  <package pattern="Internal.*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """;

        AssertEx.WhitespaceInvariantEqual( expectedMergedConfig, mergedConfig );
    }

    [Fact]
    public void RelativePathsAreResolvedToAbsolute()
    {
        using var testContext = this.CreateTestContext();

        const string parentConfig = """
                                    <configuration>
                                        <packageSources>
                                            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                            <add key="LocalFeed" value="packages/local" />
                                        </packageSources>
                                    </configuration>
                                    """;

        const string childConfig = """
                                   <configuration>
                                       <packageSources>
                                           <add key="ChildFeed" value="artifacts/publish" />
                                       </packageSources>
                                   </configuration>
                                   """;

        var path1 = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path1, parentConfig );
        var subdir = Path.Combine( testContext.BaseDirectory, "sub" );
        Directory.CreateDirectory( subdir );
        var path2 = Path.Combine( subdir, "nuget.config" );
        File.WriteAllText( path2, childConfig );

        var mergedConfig = MergeConfigFiles( testContext, path2 ).ToString();

        // Relative paths should be resolved to absolute paths based on each config file's directory.
        var resolvedParentPath = Path.GetFullPath( Path.Combine( testContext.BaseDirectory, "packages/local" ) );
        var resolvedChildPath = Path.GetFullPath( Path.Combine( subdir, "artifacts/publish" ) );

        var expectedMergedConfig =
            $"""
             <configuration>
               <packageSources>
                 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                 <add key="LocalFeed" value="{resolvedParentPath}" />
                 <add key="ChildFeed" value="{resolvedChildPath}" />
               </packageSources>
             </configuration>
             """;

        AssertEx.WhitespaceInvariantEqual( expectedMergedConfig, mergedConfig );
    }

    [Fact]
    public void ConsolidatedPackageSourceMappingClearRemovesAllInheritedMappings()
    {
        // Reproduces the Metalama.Consolidated + Metalama scenario where:
        // - Parent (Consolidated) has <packageSourceMapping> with many entries but NO <clear/>
        // - Child (Metalama) has <packageSourceMapping> with <clear/> then its own entries
        using var testContext = this.CreateTestContext();

        const string parentConfig = """
                                    <configuration>
                                        <packageSources>
                                            <clear />
                                            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                            <add key="dotnet-preview" value="https://www.myget.org/F/roslyn-consolidated/api/v3/index.json" />
                                            <add key="Metalama.Consolidated" value="artifacts/publish/private" />
                                            <add key="Metalama" value="artifacts/metalama" />
                                            <add key="Metalama.Premium" value="artifacts/premium" />
                                        </packageSources>
                                        <packageSourceMapping>
                                            <packageSource key="nuget.org">
                                                <package pattern="*" />
                                            </packageSource>
                                            <packageSource key="dotnet-preview">
                                                <package pattern="Microsoft.CodeAnalysis.*" />
                                            </packageSource>
                                            <packageSource key="Metalama.Consolidated">
                                                <package pattern="Metalama.Consolidated" />
                                                <package pattern="Metalama.Consolidated.*" />
                                            </packageSource>
                                            <packageSource key="Metalama">
                                                <package pattern="Metalama.Backstage*" />
                                                <package pattern="Metalama.Framework*" />
                                            </packageSource>
                                            <packageSource key="Metalama.Premium">
                                                <package pattern="Metalama.Extensions.Architecture" />
                                                <package pattern="Metalama.Licensing" />
                                            </packageSource>
                                        </packageSourceMapping>
                                    </configuration>
                                    """;

        const string childConfig = """
                                   <configuration>
                                       <packageSources>
                                           <clear />
                                           <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                           <add key="Metalama" value="artifacts/publish/private" />
                                       </packageSources>
                                       <packageSourceMapping>
                                           <clear />
                                           <packageSource key="nuget.org">
                                               <package pattern="*" />
                                           </packageSource>
                                           <packageSource key="Metalama">
                                               <package pattern="Metalama.Backstage*" />
                                               <package pattern="Metalama.Framework*" />
                                               <package pattern="Metalama.Patterns.*" />
                                               <package pattern="Metalama.Testing.*" />
                                           </packageSource>
                                       </packageSourceMapping>
                                   </configuration>
                                   """;

        var path1 = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path1, parentConfig );
        var subdir = Path.Combine( testContext.BaseDirectory, "sub" );
        Directory.CreateDirectory( subdir );
        var path2 = Path.Combine( subdir, "nuget.config" );
        File.WriteAllText( path2, childConfig );

        var mergedConfig = MergeConfigFiles( testContext, path2 ).ToString();

        // After <clear/>, only the child's entries should be present.
        // Relative paths are resolved to absolute paths based on the config file's directory.
        var resolvedChildPath = Path.GetFullPath( Path.Combine( subdir, "artifacts/publish/private" ) );

        var expectedMergedConfig =
            $"""
             <configuration>
               <packageSources>
                 <clear />
                 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                 <add key="Metalama" value="{resolvedChildPath}" />
               </packageSources>
               <packageSourceMapping>
                 <clear />
                 <packageSource key="nuget.org">
                   <package pattern="*" />
                 </packageSource>
                 <packageSource key="Metalama">
                   <package pattern="Metalama.Backstage*" />
                   <package pattern="Metalama.Framework*" />
                   <package pattern="Metalama.Patterns.*" />
                   <package pattern="Metalama.Testing.*" />
                 </packageSource>
               </packageSourceMapping>
             </configuration>
             """;

        AssertEx.WhitespaceInvariantEqual( expectedMergedConfig, mergedConfig );
    }

    // Constants of issue #1885: the package source that serves the prerelease Roslyn packages, and the pattern
    // under which it is mapped.
    private const string _prereleaseSourceKey = "roslyn-consolidated";
    private const string _prereleaseSourceUrl = "https://proget.postsharp.net/nuget/roslyn-consolidated/v3/index.json";
    private const string _codeAnalysisPattern = "Microsoft.CodeAnalysis.*";

    /// <summary>
    /// Creates the <see cref="NuGetHelper"/> under test, which reads the file system and the environment through the
    /// services of the test context.
    /// </summary>
    private static NuGetHelper CreateNuGetHelper( TestContext testContext ) => new( testContext.ServiceProvider.Global );

    private static XDocument MergeConfigFiles( TestContext testContext, string path )
    {
        var nuGetHelper = CreateNuGetHelper( testContext );

        return nuGetHelper.MergeConfigFiles( nuGetHelper.GetConfigFiles( path ) ).AssertNotNull();
    }

    private static string WriteConfigFile( string directory, string content )
    {
        var path = Path.Combine( directory, "nuget.config" );
        File.WriteAllText( path, content );

        return path;
    }

    [Fact]
    public void PackageSourceIsAddedWhenNoConfigFileExists()
    {
        // Issue #1885: when the user project has no nuget.config at all, the generated configuration must still
        // declare the package source that serves the prerelease Roslyn packages.
        using var testContext = this.CreateTestContext();

        var document = new XDocument( new XElement( "configuration" ) );

        var result = CreateNuGetHelper( testContext ).AddPackageSource(
            document,
            _prereleaseSourceKey,
            _prereleaseSourceUrl,
            _codeAnalysisPattern,
            Array.Empty<string>() );

        Assert.False( result.IsMappingWritten );

        const string expected = """
                                <configuration>
                                  <packageSources>
                                    <add key="roslyn-consolidated" value="https://proget.postsharp.net/nuget/roslyn-consolidated/v3/index.json" />
                                  </packageSources>
                                </configuration>
                                """;

        AssertEx.WhitespaceInvariantEqual( expected, document.ToString() );
    }

    [Fact]
    public void PackageSourceIsAddedAfterClearElement()
    {
        // Issue #1885: a clear element removes every source declared before it, so the added source must come after it.
        using var testContext = this.CreateTestContext();

        var configPath = WriteConfigFile(
            testContext.BaseDirectory,
            """
            <configuration>
                <packageSources>
                    <clear />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """ );

        var document = MergeConfigFiles( testContext, configPath );

        CreateNuGetHelper( testContext ).AddPackageSource( document, _prereleaseSourceKey, _prereleaseSourceUrl, _codeAnalysisPattern, Array.Empty<string>() );

        var packageSources = document.Root.AssertNotNull().Element( "packageSources" ).AssertNotNull();
        var elementNames = packageSources.Elements().Select( e => e.Name.LocalName ).ToList();

        Assert.Equal( new[] { "clear", "add", "add" }, elementNames );

        var addedElement = packageSources.Elements( "add" ).First( e => e.Attribute( "key" )?.Value == _prereleaseSourceKey );

        Assert.Equal( _prereleaseSourceUrl, addedElement.Attribute( "value" ).AssertNotNull().Value );
    }

    [Fact]
    public void NoMappingIsWrittenWhenTheConfigurationHasNoPackageSourceMapping()
    {
        // Issue #1885: writing a mapping when none exists would activate package source mapping for every package.
        using var testContext = this.CreateTestContext();

        var configPath = WriteConfigFile(
            testContext.BaseDirectory,
            """
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """ );

        var document = MergeConfigFiles( testContext, configPath );

        var result = CreateNuGetHelper( testContext ).AddPackageSource(
            document,
            _prereleaseSourceKey,
            _prereleaseSourceUrl,
            _codeAnalysisPattern,
            Array.Empty<string>() );

        Assert.False( result.IsMappingWritten );
        Assert.Null( result.ConflictingPattern );
        Assert.Null( document.Root.AssertNotNull().Element( "packageSourceMapping" ) );
    }

    [Fact]
    public void MappingIsWrittenAndTheStarSourceKeepsServingTheCodeAnalysisPackages()
    {
        // Issue #1885: NuGet resolves a package identifier through the longest matching pattern. Adding
        // Microsoft.CodeAnalysis.* under the prerelease source alone would make that source the only candidate,
        // so every source that covers the pattern today through a shorter one receives it as well.
        using var testContext = this.CreateTestContext();

        var configPath = WriteConfigFile(
            testContext.BaseDirectory,
            """
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
                <packageSourceMapping>
                    <packageSource key="nuget.org">
                        <package pattern="*" />
                    </packageSource>
                </packageSourceMapping>
            </configuration>
            """ );

        var document = MergeConfigFiles( testContext, configPath );

        var result = CreateNuGetHelper( testContext ).AddPackageSource(
            document,
            _prereleaseSourceKey,
            _prereleaseSourceUrl,
            _codeAnalysisPattern,
            Array.Empty<string>() );

        Assert.True( result.IsMappingWritten );

        var mapping = document.Root.AssertNotNull().Element( "packageSourceMapping" ).AssertNotNull();

        var nugetOrgPatterns = mapping.Elements( "packageSource" )
            .First( e => e.Attribute( "key" )?.Value == "nuget.org" )
            .Elements( "package" )
            .Select( e => e.Attribute( "pattern" )?.Value )
            .ToList();

        Assert.Equal( new[] { "*", _codeAnalysisPattern }, nugetOrgPatterns );

        var prereleasePatterns = mapping.Elements( "packageSource" )
            .First( e => e.Attribute( "key" )?.Value == _prereleaseSourceKey )
            .Elements( "package" )
            .Select( e => e.Attribute( "pattern" )?.Value )
            .ToList();

        Assert.Equal( new[] { _codeAnalysisPattern }, prereleasePatterns );
    }

    [Fact]
    public void NoMappingIsWrittenWhenTheUserAlreadyMapsTheSamePattern()
    {
        // Issue #1885: the user has expressed an intention about these packages, and Metalama does not override it.
        using var testContext = this.CreateTestContext();

        var configPath = WriteConfigFile(
            testContext.BaseDirectory,
            """
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                    <add key="MyFeed" value="https://myfeed.example.com/index.json" />
                </packageSources>
                <packageSourceMapping>
                    <packageSource key="nuget.org">
                        <package pattern="*" />
                    </packageSource>
                    <packageSource key="MyFeed">
                        <package pattern="Microsoft.CodeAnalysis.*" />
                    </packageSource>
                </packageSourceMapping>
            </configuration>
            """ );

        var document = MergeConfigFiles( testContext, configPath );

        var result = CreateNuGetHelper( testContext ).AddPackageSource(
            document,
            _prereleaseSourceKey,
            _prereleaseSourceUrl,
            _codeAnalysisPattern,
            Array.Empty<string>() );

        Assert.False( result.IsMappingWritten );
        Assert.Equal( "Microsoft.CodeAnalysis.*", result.ConflictingPattern );
        Assert.Equal( "MyFeed", result.ConflictingSourceKey );

        var mapping = document.Root.AssertNotNull().Element( "packageSourceMapping" ).AssertNotNull();

        Assert.DoesNotContain( mapping.Elements( "packageSource" ), e => e.Attribute( "key" )?.Value == _prereleaseSourceKey );

        var nugetOrgPatterns = mapping.Elements( "packageSource" )
            .First( e => e.Attribute( "key" )?.Value == "nuget.org" )
            .Elements( "package" )
            .Select( e => e.Attribute( "pattern" )?.Value )
            .ToList();

        Assert.Equal( new[] { "*" }, nugetOrgPatterns );

        // The source itself is still declared, because the user may serve the packages from the same address under
        // another key.
        Assert.Contains(
            document.Root.AssertNotNull().Element( "packageSources" ).AssertNotNull().Elements( "add" ),
            e => e.Attribute( "key" )?.Value == _prereleaseSourceKey );
    }

    [Fact]
    public void NoMappingIsWrittenWhenTheUserAlreadyMapsAMoreSpecificPattern()
    {
        // Issue #1885: a pattern whose literal prefix starts with the literal prefix of Microsoft.CodeAnalysis.* is
        // more specific, so it also expresses an intention about these packages.
        using var testContext = this.CreateTestContext();

        var configPath = WriteConfigFile(
            testContext.BaseDirectory,
            """
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                    <add key="MyFeed" value="https://myfeed.example.com/index.json" />
                </packageSources>
                <packageSourceMapping>
                    <packageSource key="nuget.org">
                        <package pattern="*" />
                    </packageSource>
                    <packageSource key="MyFeed">
                        <package pattern="Microsoft.CodeAnalysis.CSharp" />
                    </packageSource>
                </packageSourceMapping>
            </configuration>
            """ );

        var document = MergeConfigFiles( testContext, configPath );

        var result = CreateNuGetHelper( testContext ).AddPackageSource(
            document,
            _prereleaseSourceKey,
            _prereleaseSourceUrl,
            _codeAnalysisPattern,
            Array.Empty<string>() );

        Assert.False( result.IsMappingWritten );
        Assert.Equal( "Microsoft.CodeAnalysis.CSharp", result.ConflictingPattern );
        Assert.Equal( "MyFeed", result.ConflictingSourceKey );
    }

    [Fact]
    public void MappingDecisionAccountsForTheUserLevelConfigFile()
    {
        // Issue #1885: NuGet reads the user-level configuration file for the temporary project as well, because that
        // file is not tied to a directory tree. A packageSourceMapping section declared only there must therefore take
        // part in the decision, and the source that covers the pattern through a shorter one must keep every pattern
        // it declares, because a packageSource element of the same key replaces the inherited one.
        using var testContext = this.CreateTestContext();

        var projectDirectory = Path.Combine( testContext.BaseDirectory, "project" );
        Directory.CreateDirectory( projectDirectory );

        var configPath = WriteConfigFile(
            projectDirectory,
            """
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """ );

        var userDirectory = Path.Combine( testContext.BaseDirectory, "user" );
        Directory.CreateDirectory( userDirectory );

        var userConfigPath = WriteConfigFile(
            userDirectory,
            """
            <configuration>
                <packageSourceMapping>
                    <packageSource key="nuget.org">
                        <package pattern="*" />
                        <package pattern="System.*" />
                    </packageSource>
                </packageSourceMapping>
            </configuration>
            """ );

        var document = MergeConfigFiles( testContext, configPath );

        Assert.Null( document.Root.AssertNotNull().Element( "packageSourceMapping" ) );

        var result = CreateNuGetHelper( testContext ).AddPackageSource(
            document,
            _prereleaseSourceKey,
            _prereleaseSourceUrl,
            _codeAnalysisPattern,
            new[] { userConfigPath } );

        Assert.True( result.IsMappingWritten );

        var mapping = document.Root.AssertNotNull().Element( "packageSourceMapping" ).AssertNotNull();

        var nugetOrgPatterns = mapping.Elements( "packageSource" )
            .First( e => e.Attribute( "key" )?.Value == "nuget.org" )
            .Elements( "package" )
            .Select( e => e.Attribute( "pattern" )?.Value )
            .ToList();

        Assert.Equal( new[] { "*", "System.*", _codeAnalysisPattern }, nugetOrgPatterns );

        var prereleasePatterns = mapping.Elements( "packageSource" )
            .First( e => e.Attribute( "key" )?.Value == _prereleaseSourceKey )
            .Elements( "package" )
            .Select( e => e.Attribute( "pattern" )?.Value )
            .ToList();

        Assert.Equal( new[] { _codeAnalysisPattern }, prereleasePatterns );
    }

    [Fact]
    public void CurrentRoslynVersionHasNoPrereleasePackageSource()
    {
        // Issue #1885: this branch compiles against a released Roslyn, so no package source is declared and the
        // generated nuget.config is what it was before. This test fails when a version branch moves onto a prerelease
        // Roslyn, which is the point at which the switch has to be reviewed.
        Assert.Null( RoslynApiVersion.Current.ToPrereleasePackageSourceUrl() );
    }

    /// <summary>
    /// Creates the <see cref="NuGetHelper"/> under test with a given environment, so that the resolution of the
    /// user-level configuration file does not depend on the machine that runs the test.
    /// </summary>
    private static NuGetHelper CreateNuGetHelper( TestContext testContext, IEnvironmentVariableProvider environmentVariables )
        => new( testContext.ServiceProvider.Global.GetRequiredBackstageService<IFileSystem>(), environmentVariables );

    /// <summary>
    /// Creates a directory under the base directory of the test context and writes a NuGet configuration file into it,
    /// under the name that the NuGet tools give it, and returns the path of that file.
    /// </summary>
    private static string WriteUserConfigFile( TestContext testContext, params string[] directoryParts )
    {
        var directory = Path.Combine( new[] { testContext.BaseDirectory }.Concat( directoryParts ).ToArray() );
        Directory.CreateDirectory( directory );

        var path = Path.Combine( directory, "NuGet.Config" );
        File.WriteAllText( path, "<configuration />" );

        return path;
    }

    [Fact]
    public void NoUserConfigFileIsFoundWhenTheEnvironmentDefinesNoDirectory()
    {
        // Issue #1885: an environment in which no candidate directory exists yields no file, and the mapping decision
        // is then taken from the discovered configuration files alone.
        using var testContext = this.CreateTestContext();

        var environmentVariables = new TestEnvironmentVariableProvider();

        Assert.Null( CreateNuGetHelper( testContext, environmentVariables ).GetUserConfigFile() );
    }

    [Fact]
    public void UserConfigFileIsFoundInTheApplicationDataDirectory()
    {
        // Issue #1885: on Windows the user-level configuration file is under %APPDATA%\NuGet, and its name is spelled
        // NuGet.Config, which the lookup has to match without regard to case.
        using var testContext = this.CreateTestContext();

        var expectedPath = WriteUserConfigFile( testContext, "AppData", "NuGet" );

        var environmentVariables = new TestEnvironmentVariableProvider();
        environmentVariables.Environment["APPDATA"] = Path.Combine( testContext.BaseDirectory, "AppData" );

        Assert.Equal( expectedPath, CreateNuGetHelper( testContext, environmentVariables ).GetUserConfigFile() );
    }

    [Fact]
    public void UserConfigFileIsFoundUnderTheConfigurationHomeDirectory()
    {
        // Issue #1885: on Unix the application data directory is $XDG_CONFIG_HOME when that variable is defined.
        using var testContext = this.CreateTestContext();

        var expectedPath = WriteUserConfigFile( testContext, "config", "NuGet" );

        var environmentVariables = new TestEnvironmentVariableProvider();
        environmentVariables.Environment["XDG_CONFIG_HOME"] = Path.Combine( testContext.BaseDirectory, "config" );

        Assert.Equal( expectedPath, CreateNuGetHelper( testContext, environmentVariables ).GetUserConfigFile() );
    }

    [Fact]
    public void UserConfigFileIsFoundUnderTheHomeDirectoryWhenNoConfigurationHomeIsDefined()
    {
        // Issue #1885: on Unix the application data directory is $HOME/.config when $XDG_CONFIG_HOME is not defined.
        using var testContext = this.CreateTestContext();

        var expectedPath = WriteUserConfigFile( testContext, "home", ".config", "NuGet" );

        var environmentVariables = new TestEnvironmentVariableProvider();
        environmentVariables.Environment["HOME"] = Path.Combine( testContext.BaseDirectory, "home" );

        Assert.Equal( expectedPath, CreateNuGetHelper( testContext, environmentVariables ).GetUserConfigFile() );
    }

    [Fact]
    public void UserConfigFileIsFoundInTheLegacyDirectory()
    {
        // Issue #1885: NuGet also reads the file under the home directory of the user, which is the location it used
        // before the application data directory.
        using var testContext = this.CreateTestContext();

        var expectedPath = WriteUserConfigFile( testContext, "profile", ".nuget", "NuGet" );

        var environmentVariables = new TestEnvironmentVariableProvider();
        environmentVariables.Environment["USERPROFILE"] = Path.Combine( testContext.BaseDirectory, "profile" );

        Assert.Equal( expectedPath, CreateNuGetHelper( testContext, environmentVariables ).GetUserConfigFile() );
    }

    [Fact]
    public void TheApplicationDataDirectoryIsProbedBeforeTheLegacyDirectory()
    {
        // Issue #1885: NuGet probes the application data directory first, so a file in the legacy directory is read
        // only when the application data directory holds none.
        using var testContext = this.CreateTestContext();

        var expectedPath = WriteUserConfigFile( testContext, "AppData", "NuGet" );
        WriteUserConfigFile( testContext, "profile", ".nuget", "NuGet" );

        var environmentVariables = new TestEnvironmentVariableProvider();
        environmentVariables.Environment["APPDATA"] = Path.Combine( testContext.BaseDirectory, "AppData" );
        environmentVariables.Environment["USERPROFILE"] = Path.Combine( testContext.BaseDirectory, "profile" );

        Assert.Equal( expectedPath, CreateNuGetHelper( testContext, environmentVariables ).GetUserConfigFile() );
    }
}
