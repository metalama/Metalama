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
}