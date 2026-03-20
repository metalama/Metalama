// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public class NuGetHelperTests : UnitTestClass
{
    [Fact]
    public void Test()
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
    public void FallbackPackageFoldersRelativePathsAreResolvedToAbsolute()
    {
        using var testContext = this.CreateTestContext();

        const string parentConfig = """
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

        var path1 = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path1, parentConfig );

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path1 ) ).AssertNotNull().ToString();

        // The relative path "nuget/fallback" should be resolved relative to the config file's directory.
        var resolvedFallbackPath = Path.GetFullPath( Path.Combine( testContext.BaseDirectory, "nuget/fallback" ) );

        var expectedMergedConfig =
            $"""
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <fallbackPackageFolders>
                <add key="SomeFallback" value="{resolvedFallbackPath}" />
              </fallbackPackageFolders>
            </configuration>
            """;

        AssertEx.WhitespaceInvariantEqual( expectedMergedConfig, mergedConfig );
    }

    [Fact]
    public void ConfigRepositoryPathRelativePathIsResolvedToAbsolute()
    {
        using var testContext = this.CreateTestContext();

        const string config = """
                              <configuration>
                                  <config>
                                      <add key="repositoryPath" value="packages/installed" />
                                      <add key="globalPackagesFolder" value="cache/global" />
                                      <add key="defaultPushSource" value="https://MyRepo/api/v2/package" />
                                  </config>
                              </configuration>
                              """;

        var path = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path, config );

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path ) ).AssertNotNull().ToString();

        var resolvedRepoPath = Path.GetFullPath( Path.Combine( testContext.BaseDirectory, "packages/installed" ) );
        var resolvedGlobalPath = Path.GetFullPath( Path.Combine( testContext.BaseDirectory, "cache/global" ) );

        var expectedMergedConfig =
            $"""
            <configuration>
              <config>
                <add key="repositoryPath" value="{resolvedRepoPath}" />
                <add key="globalPackagesFolder" value="{resolvedGlobalPath}" />
                <add key="defaultPushSource" value="https://MyRepo/api/v2/package" />
              </config>
            </configuration>
            """;

        AssertEx.WhitespaceInvariantEqual( expectedMergedConfig, mergedConfig );
    }

    [Fact]
    public void EnvironmentVariablePathsAreNotResolved()
    {
        using var testContext = this.CreateTestContext();

        const string config = """
                              <configuration>
                                  <config>
                                      <add key="repositoryPath" value="%PACKAGEHOME%/External" />
                                  </config>
                                  <packageSources>
                                      <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                  </packageSources>
                                  <fallbackPackageFolders>
                                      <add key="EnvFallback" value="%NUGET_FALLBACK%/packages" />
                                  </fallbackPackageFolders>
                              </configuration>
                              """;

        var path = Path.Combine( testContext.BaseDirectory, "nuget.config" );
        File.WriteAllText( path, config );

        var mergedConfig = NuGetHelper.MergeConfigFiles( NuGetHelper.GetConfigFiles( path ) ).AssertNotNull().ToString();

        // Values containing environment variables (%VAR%) should NOT be resolved.
        var expectedMergedConfig =
            """
            <configuration>
              <config>
                <add key="repositoryPath" value="%PACKAGEHOME%/External" />
              </config>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <fallbackPackageFolders>
                <add key="EnvFallback" value="%NUGET_FALLBACK%/packages" />
              </fallbackPackageFolders>
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