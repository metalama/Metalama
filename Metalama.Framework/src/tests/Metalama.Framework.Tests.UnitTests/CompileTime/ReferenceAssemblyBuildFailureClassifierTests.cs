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

    /// <summary>
    /// NuGet reports the rejected request without a message identifier, so the HTTP status code is the only invariant
    /// signal. It is recognized in a localized output, where every word around it differs.
    /// </summary>
    [Theory]
    [InlineData( "TempProject.csproj(5): error :   Response status code does not indicate success: 401 (Unauthorized)." )]
    [InlineData( "TempProject.csproj(5): erreur :   Le code d'état de la réponse n'indique pas une réussite : 401 (Unauthorized)." )]
    public void ProbableCause_FeedRequiresCredentials( string statusLine )
    {
        var output = ImmutableArray.Create(
            "MSBuild version 17.8.43+f0cbb1397 for .NET",
            "TempProject.csproj(5): error : Unable to load the service index for source https://feed.example.com/v3/index.json.",
            statusLine );

        Assert.Contains( "HTTP authentication error", GetProbableCause( output ) );
    }

    /// <summary>
    /// The status code is recognized only in an output that also contains a URL, and only as a whole number, so that a
    /// package version or a build number that ends in 401 does not produce a message about credentials.
    /// </summary>
    [Theory]
    [InlineData( "TempProject.csproj(5): error : Restore failed for https://feed.example.com after 1.0.401 seconds." )]
    [InlineData( "TempProject.csproj(5): error : Package Some.Package 3.401.0 could not be installed." )]
    public void ProbableCause_NumberResemblingAStatusCodeIsNotAnAuthenticationFailure( string line )
    {
        Assert.Contains( "not a defect of Metalama", GetProbableCause( ImmutableArray.Create( line ) ) );
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

    /// <summary>
    /// The .NET host fails before MSBuild is loaded, so the output carries no message identifier and the file name
    /// <c>global.json</c> is the only invariant signal. The prose around it differs in a localized toolchain.
    /// </summary>
    [Theory]
    [InlineData( "      A compatible .NET SDK was not found.", "Requested SDK version: 9.0.311" )]
    [InlineData( "      Aucun SDK .NET compatible n'a été trouvé.", "Version du SDK demandée : 9.0.311" )]
    public void ProbableCause_SdkPinnedByGlobalJsonIsNotInstalled( string notFoundLine, string requestedVersionLine )
    {
        var output = ImmutableArray.Create( notFoundLine, requestedVersionLine, "global.json file: C:\\src\\global.json" );

        Assert.Contains( "This build resolved a global.json file", GetProbableCause( output ) );
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

    /// <summary>
    /// The .NET SDK reports an MSBuild that is too old for it without any message identifier, and the sentence that
    /// carries the two versions is localized, so the condition carries no invariant signal and is deliberately left
    /// unclassified. The quoted output still shows the user what happened. See #1744.
    /// </summary>
    [Fact]
    public void ProbableCause_MSBuildTooOldForSdkIsLeftUnclassified()
    {
        var output = ImmutableArray.Create(
            "Version 10.0.201 of the .NET SDK requires at least version 18.0.0 of MSBuild. The current available version of MSBuild is 17.14.23.42201." );

        Assert.Contains( "not a defect of Metalama", GetProbableCause( output ) );
        Assert.Contains( "requires at least version 18.0.0 of MSBuild", ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output ) );
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

    /// <summary>
    /// An output that carries no message identifier cannot be filtered down to its error lines without depending on the
    /// language of the toolchain, so the last lines are quoted instead. They are where a failing build explains itself,
    /// and the user reads them in the language of their own toolchain.
    /// </summary>
    [Fact]
    public void ReportedErrors_LocalizedOutputWithoutMessageIdentifierFallsBackToTheLastLines()
    {
        var output = ImmutableArray.Create(
            "Version de MSBuild 17.8.43+f0cbb1397 pour .NET",
            "  Determination des projets a restaurer...",
            "TempProject.csproj(5): erreur :   Le code d'etat de la reponse n'indique pas une reussite : 401 (Unauthorized)." );

        var errors = ReferenceAssemblyBuildFailureClassifier.GetReportedErrors( output );

        Assert.Contains( "Its last output lines were the following:", errors );
        Assert.Contains( "401 (Unauthorized)", errors );
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
