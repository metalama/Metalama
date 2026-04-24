// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System.IO;
using System.Linq;
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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( configPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( childPath ) ).AssertNotNull();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path2 ) ).AssertNotNull().ToString();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path2 ) ).AssertNotNull().ToString();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path2 ) ).AssertNotNull().ToString();

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

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path2 ) ).AssertNotNull().ToString();

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
}