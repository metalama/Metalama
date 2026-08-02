// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests <see cref="ReferenceAssemblyBuildFailureClassifier"/>, which turns the console output of the failed nested
/// reference-assembly build into the text of a diagnostic.
/// </summary>
/// <remarks>
/// The outputs used here are modelled on the ones reported in issues #1744, #1745, #1746 and #1747.
/// </remarks>
public sealed class ReferenceAssemblyBuildFailureClassifierTests
{
    private const string _projectDirectory = @"C:\Temp\Metalama\AssemblyLocator\abcdef";

    /// <summary>
    /// Calls <see cref="ReferenceAssemblyBuildFailureClassifier.GetProbableCause"/> for a build in which Metalama pinned
    /// no .NET SDK version, which is the ordinary case.
    /// </summary>
    private static string GetProbableCause( ImmutableArray<string> output )
        => ReferenceAssemblyBuildFailureClassifier.GetProbableCause( output, _projectDirectory, null );

    [Fact]
    public void ProbableCause_FeedRequiresCredentials()
    {
        var output = ImmutableArray.Create(
            "MSBuild version 17.8.43+f0cbb1397 for .NET",
            "  Determining projects to restore...",
            "TempProject.csproj(5): error : Unable to load the service index for source https://feed.example.com/v3/index.json.",
            "TempProject.csproj(5): error :   Response status code does not indicate success: 401 (Unauthorized)." );

        Assert.Contains( "credentials", GetProbableCause( output ) );
    }

    [Fact]
    public void ProbableCause_InsecureFeed()
    {
        var output = ImmutableArray.Create( "TempProject.csproj(5): error NU1302: The source 'http://feed' uses an insecure connection." );

        Assert.Contains( "allowInsecureConnections", GetProbableCause( output ) );
    }

    [Fact]
    public void ProbableCause_PackageNotFoundOnMappedSource()
    {
        var output = ImmutableArray.Create(
            "TempProject.csproj(5): error NU1101: Unable to find package Microsoft.CodeAnalysis.CSharp.",
            "TempProject.csproj(5): error NU1101: No packages exist with this id in source(s): InternalFeed" );

        Assert.Contains( "packageSourceMapping", GetProbableCause( output ) );
    }

    [Fact]
    public void ProbableCause_SdkPinnedByGlobalJsonIsNotInstalled()
    {
        var output = ImmutableArray.Create(
            "The command could not be loaded, possibly because:",
            "  * You intended to execute a .NET SDK command:",
            "      A compatible .NET SDK was not found.",
            "",
            "Requested SDK version: 9.0.311",
            "global.json file: C:\\src\\global.json" );

        Assert.Contains( "global.json", GetProbableCause( output ) );
    }

    /// <summary>
    /// Metalama writes its own global.json beside the reference-assembly project to pin the .NET SDK to the version that
    /// builds the project. When that is the file at fault, the user must not be sent looking at their own global.json.
    /// </summary>
    [Fact]
    public void ProbableCause_SdkPinnedByMetalamaIsNotInstalled()
    {
        var output = ImmutableArray.Create(
            "      A compatible .NET SDK was not found.",
            "Requested SDK version: 9.0.311",
            $"global.json file: {_projectDirectory}\\global.json" );

        var probableCause = ReferenceAssemblyBuildFailureClassifier.GetProbableCause( output, _projectDirectory, "9.0.311" );

        Assert.Contains( "Metalama requested the .NET SDK version '9.0.311'", probableCause );
    }

    [Fact]
    public void ProbableCause_SdkTooOldForTargetFramework()
    {
        var output = ImmutableArray.Create( "TempProject.csproj(1,1): error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0." );

        Assert.Contains( "MetalamaCompileTimeTargetFrameworks", GetProbableCause( output ) );
    }

    [Fact]
    public void ProbableCause_MSBuildTooOldForSdk()
    {
        var output = ImmutableArray.Create(
            "Version 10.0.201 of the .NET SDK requires at least version 18.0.0 of MSBuild. The current available version of MSBuild is 17.14.23.42201." );

        Assert.Contains( "older than the version required by the .NET SDK", GetProbableCause( output ) );
    }

    [Fact]
    public void ProbableCause_TaskCrashedWithAccessViolation()
    {
        var output = ImmutableArray.Create(
            "TempProject.csproj(1,1): error MSB6006: \"csc.exe\" exited with code -1073741819 [C:\\temp\\TempProject.csproj::TargetFramework=netstandard2.0]" );

        Assert.Contains( "access violation", GetProbableCause( output ) );
    }

    [Fact]
    public void ProbableCause_UnrecognizedOutputFallsBackToTheGenericSentence()
    {
        var output = ImmutableArray.Create( "Build FAILED." );

        Assert.Contains( "not a defect of Metalama", GetProbableCause( output ) );
    }

    /// <summary>
    /// Verifies that the message identifier is recognized even when MSBuild localizes the prose that surrounds it, which
    /// is the case for a substantial share of the affected users.
    /// </summary>
    [Fact]
    public void ReportedErrors_LocalizedOutputIsRecognizedByItsMessageIdentifier()
    {
        var output = ImmutableArray.Create( "TempProject.csproj(5): erreur NU1101: Impossible de trouver le package Microsoft.CodeAnalysis.CSharp." );

        Assert.Contains( "NU1101", ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output ) );
    }

    [Fact]
    public void ReportedErrors_NonErrorLinesAreExcluded()
    {
        var output = ImmutableArray.Create(
            "MSBuild version 17.8.43+f0cbb1397 for .NET",
            "  Determining projects to restore...",
            "TempProject.csproj(5): error NU1101: Unable to find package Microsoft.CodeAnalysis.CSharp.",
            "Build FAILED." );

        var errors = ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output );

        Assert.Contains( "NU1101", errors );
        Assert.DoesNotContain( "Determining projects", errors );
        Assert.DoesNotContain( "Build FAILED", errors );
    }

    /// <summary>
    /// A multi-targeted build reports the same error once per target framework, and MSBuild appends the name of the
    /// target framework to each occurrence. Quoting all of them would fill the diagnostic without adding information.
    /// </summary>
    [Fact]
    public void ReportedErrors_ErrorsRepeatedPerTargetFrameworkAreQuotedOnce()
    {
        var output = ImmutableArray.Create(
            "csc : error MSB6006: \"csc.exe\" exited with code -1073741819 [C:\\temp\\TempProject.csproj::TargetFramework=netstandard2.0]",
            "csc : error MSB6006: \"csc.exe\" exited with code -1073741819 [C:\\temp\\TempProject.csproj::TargetFramework=net8.0]",
            "csc : error MSB6006: \"csc.exe\" exited with code -1073741819 [C:\\temp\\TempProject.csproj::TargetFramework=net48]" );

        var errors = ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output );

        Assert.Equal( "It reported the following errors: " + output[0], errors );
    }

    /// <summary>
    /// Verifies that the text of the diagnostic never contains a line break, which a Roslyn diagnostic cannot carry.
    /// </summary>
    [Fact]
    public void ReportedErrors_TextHasNoLineBreak()
    {
        var output = ImmutableArray.Create(
            "TempProject.csproj(5): error NU1101: Unable to find package\r\nMicrosoft.CodeAnalysis.CSharp.",
            "TempProject.csproj(5): error NU1102: Unable to find package\nMicrosoft.CodeAnalysis." );

        var errors = ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output );

        Assert.DoesNotContain( "\r", errors );
        Assert.DoesNotContain( "\n", errors );
    }

    /// <summary>
    /// When the child process fails before MSBuild can report a diagnostic, as when the .NET host cannot satisfy a
    /// global.json, there is no error line to quote and the last output lines are the informative ones.
    /// </summary>
    [Fact]
    public void ReportedErrors_WithoutErrorLineTheLastOutputLinesAreQuoted()
    {
        var output = ImmutableArray.Create( "A compatible .NET SDK was not found.", "", "Requested SDK version: 9.0.311" );

        var errors = ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output );

        Assert.Contains( "Requested SDK version: 9.0.311", errors );
    }

    [Fact]
    public void ReportedErrors_EmptyOutput()
    {
        Assert.Equal( "It did not produce any output.", ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( ImmutableArray<string>.Empty ) );
    }
}
