// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Metrics;
using Metalama.Framework.Workspaces;
using Metalama.Testing.UnitTesting;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable VSTHRD200

namespace Metalama.Extensions.Metrics.UnitTests
{
    public sealed class AddMetricsTests : UnitTestClass
    {
        private static readonly ImmutableDictionary<string, string> _buildProperties = ImmutableDictionary<string, string>.Empty
            .Add( "DOTNET_ROOT_X64", "" )
            .Add( "MSBUILD_EXE_PATH", "" )
            .Add( "MSBuildSDKsPath", "" );

        [Fact]
        public async Task AddMetrics_RegistersMetricProviders()
        {
            using var testContext = this.CreateTestContext();

            var projectPath = Path.Combine( testContext.BaseDirectory, "Project.csproj" );
            var codePath = Path.Combine( testContext.BaseDirectory, "Code.cs" );

            await File.WriteAllTextAsync(
                projectPath,
                @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
</Project>
" );

            await File.WriteAllTextAsync(
                codePath,
                @"
class C
{
    void M1() {}
    void M2()
    {
        var x = 0;
        x++;
    }
}
" );

            var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );
            workspaceCollection.ServiceBuilder.AddMetrics();

            using var workspace = await workspaceCollection.LoadAsync( [projectPath], _buildProperties );

            var type = workspace.SourceCode.Types.Single( t => t.Name == "C" );

            var m1 = type.Methods.Single( m => m.Name == "M1" );
            Assert.Equal( 0, m1.Metrics().Get<StatementsCount>().Value );

            var m2 = type.Methods.Single( m => m.Name == "M2" );
            Assert.Equal( 2, m2.Metrics().Get<StatementsCount>().Value );

            Assert.Equal( 2, type.Metrics().Get<StatementsCount>().Value );
            Assert.True( type.Metrics().Get<SyntaxNodesCount>().Value > 0 );
            Assert.True( type.Metrics().Get<LinesOfCode>().Logical > 0 );
        }
    }
}
