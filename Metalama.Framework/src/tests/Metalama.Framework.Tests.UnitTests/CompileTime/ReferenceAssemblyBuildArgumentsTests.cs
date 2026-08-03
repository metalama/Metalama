// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the command line of the nested build that resolves the compile-time reference assemblies.
/// </summary>
/// <remarks>
/// That build runs inside the compiler, which itself runs inside a task of the outer build, so it must not compete for
/// MSBuild worker nodes with the build that is waiting for it. Both switches that prevent this are asserted here,
/// because removing either of them reintroduces a deadlock that only manifests as an intermittent timeout on a loaded
/// machine, and therefore would not be caught by any other test. See issue #1740.
/// </remarks>
public sealed class ReferenceAssemblyBuildArgumentsTests
{
    [Fact]
    public void DotNetToolBuildRunsInASingleNonReusableNode()
    {
        var arguments = CompileTimeAssemblyLocator.GetDotNetToolArguments( "msbuild_1234.binlog" );

        Assert.Equal( "build -nodeReuse:false -m:1 -bl:msbuild_1234.binlog", arguments );
    }

    [Fact]
    public void MSBuildToolBuildRunsInASingleNonReusableNode()
    {
        var arguments = CompileTimeAssemblyLocator.GetMSBuildToolArguments( @"C:\cache\TempProject.csproj", "msbuild_1234.binlog" );

        Assert.Equal( @"""C:\cache\TempProject.csproj"" /t:Restore;Build /nodeReuse:false /m:1 /bl:msbuild_1234.binlog", arguments );
    }
}
