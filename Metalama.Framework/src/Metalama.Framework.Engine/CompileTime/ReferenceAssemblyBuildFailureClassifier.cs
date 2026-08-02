// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Interprets the console output of the nested build that <see cref="CompileTimeAssemblyLocator"/> runs to resolve the
/// compile-time reference assemblies, so that its failures can be reported as an actionable diagnostic instead of an
/// unexpected exception.
/// </summary>
/// <remarks>
/// <para>
/// Practically all failures of that build are caused by the environment (the .NET SDK installation, the NuGet
/// configuration or the network) rather than by a defect in Metalama, but the child process has already explained the
/// failure in its own output. See issue #1744.
/// </para>
/// <para>
/// The output is matched on message identifiers such as <c>NU1101</c> whenever possible, because MSBuild localizes the
/// prose that surrounds them and a substantial share of the affected users run a localized toolchain.
/// </para>
/// </remarks>
internal static class ReferenceAssemblyBuildFailureClassifier
{
    /// <summary>
    /// The maximal length of the text of <see cref="GetReportedErrors"/>, so that the resulting diagnostic remains
    /// readable in a build log or in the error list of an IDE.
    /// </summary>
    private const int _maxReportedErrorsLength = 600;

    /// <summary>
    /// The maximal number of error lines quoted by <see cref="GetReportedErrors"/>. A multi-targeted build repeats
    /// the same error once per target framework, so quoting all of them adds length but no information.
    /// </summary>
    private const int _maxReportedErrorCount = 4;

    /// <summary>
    /// Matches a line that carries an MSBuild, NuGet or .NET SDK message identifier followed by a colon, such as
    /// <c>error NU1101:</c>. The identifier survives localization, unlike the word <c>error</c> that precedes it.
    /// </summary>
    private static readonly Regex _messageIdRegex = new(
        @"\b(?:NU|NETSDK|MSB|CS|AD)[0-9]{3,5}\s*:",
        RegexOptions.CultureInvariant );

    /// <summary>
    /// Matches an English error line that carries no message identifier, such as the <c>error :</c> lines that NuGet
    /// emits for a transport failure.
    /// </summary>
    private static readonly Regex _englishErrorRegex = new(
        @"\berror\s*:",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );

    /// <summary>
    /// Matches a run of whitespace, used to fold the output into the single line that a Roslyn diagnostic requires.
    /// </summary>
    private static readonly Regex _whitespaceRegex = new( @"\s+", RegexOptions.CultureInvariant );

    /// <summary>
    /// Returns the sentence that describes the probable cause of the failure and the action that resolves it, or the
    /// generic sentence when the output matches none of the known conditions.
    /// </summary>
    /// <param name="output">The console output of the nested build.</param>
    /// <param name="projectDirectory">
    /// The directory of the reference-assembly project, used to determine whether a <c>global.json</c> named by the output
    /// is the one that Metalama wrote or one that belongs to the user.
    /// </param>
    /// <param name="requestedSdkVersion">
    /// The version of the .NET SDK that Metalama pinned in the <c>global.json</c> that it wrote beside the
    /// reference-assembly project, or <c>null</c> when it pinned none.
    /// </param>
    public static string GetProbableCause( ImmutableArray<string> output, string projectDirectory, string? requestedSdkVersion )
    {
        var text = string.Join( "\n", output );

        // The .NET host refuses to run before MSBuild is even loaded, so this condition is recognized by the prose of
        // the host and not by a message identifier. See issue #1745.
        if ( Contains( text, "A compatible .NET SDK was not found" ) || Contains( text, "Requested SDK version" ) )
        {
            // Metalama writes a global.json beside the reference-assembly project to pin the .NET SDK to the version that
            // builds the project. Blaming the user's global.json when the file at fault is that one would send the user
            // looking for a file that does not concern this failure.
            var isMetalamaGlobalJson = output.Any(
                line => line.IndexOf( "global.json", StringComparison.OrdinalIgnoreCase ) >= 0
                        && line.IndexOf( projectDirectory, StringComparison.OrdinalIgnoreCase ) >= 0 );

            if ( isMetalamaGlobalJson && !string.IsNullOrEmpty( requestedSdkVersion ) )
            {
                return
                    $"Metalama requested the .NET SDK version '{requestedSdkVersion}' for this build, because this is the version of the .NET SDK "
                    + "that builds the project, but that version is not available to the child process. This typically happens when the .NET SDK of "
                    + "the host, for instance the one that is bundled with the IDE, is not also installed as a stand-alone .NET SDK. Install that "
                    + "version of the .NET SDK.";
            }

            return
                "This build resolved a global.json file that requests a version of the .NET SDK that is not installed on this computer. "
                + "Install the requested version of the .NET SDK, or change global.json so that it requests a version that is installed.";
        }

        // The nested build is a separate process and therefore does not inherit the interactive credential provider of
        // the IDE or of the outer build. See issue #1744.
        if ( Contains( text, "401 (Unauthorized)" ) || Contains( text, "403 (Forbidden)" ) )
        {
            return
                "A NuGet feed refused the request because no valid credentials were supplied. This build runs in a separate process, "
                + "which does not inherit the interactive credential provider of the outer build, therefore the credentials of the feed "
                + "must be available without user interaction, typically in a nuget.config file or through a credential provider.";
        }

        if ( Contains( text, "NU1302" ) )
        {
            return
                "A NuGet feed is configured with an insecure HTTP address, which NuGet rejects by default. Give that feed an HTTPS address, "
                + "or set the allowInsecureConnections attribute of that feed in nuget.config.";
        }

        // packageSourceMapping applies to the nested build because it inherits the nuget.config of the project, and a
        // Microsoft.CodeAnalysis.* pattern is more specific than a * pattern. See issue #1747.
        if ( Contains( text, "NU1101" ) )
        {
            return
                "A package that is required to resolve the compile-time reference assemblies was not found on the configured NuGet sources. "
                + "When nuget.config maps the Microsoft.CodeAnalysis.* pattern to a private feed through packageSourceMapping, this build is "
                + "restricted to that feed, therefore these packages must be mapped to a source that provides them, such as nuget.org.";
        }

        if ( Contains( text, "NU1301" ) || Contains( text, "Unable to load the service index for source" ) )
        {
            return
                "A NuGet feed could not be reached. Verify that the feed is available from this computer and that the "
                + "network configuration of the build environment allows the build to reach it.";
        }

        if ( Contains( text, "NETSDK1045" ) )
        {
            return
                "The .NET SDK that this build resolved is too old for the target frameworks of the reference-assembly project. "
                + $"Install a more recent .NET SDK, or set the {MSBuildPropertyNames.MetalamaCompileTimeTargetFrameworks} MSBuild property "
                + "to target frameworks that this .NET SDK supports.";
        }

        if ( Contains( text, "of MSBuild" ) && Contains( text, "requires at least version" ) )
        {
            return
                "The version of MSBuild that this build used is older than the version required by the .NET SDK that it resolved. "
                + "Build with a version of MSBuild or of Visual Studio that matches the .NET SDK, or request an older .NET SDK in a "
                + "global.json file.";
        }

        // 0xC0000005, that is, STATUS_ACCESS_VIOLATION, reported by MSB6006 for a task that crashed. See issue #1746.
        if ( Contains( text, "-1073741819" ) )
        {
            return
                "A tool started by this build terminated abnormally with an access violation. This is a failure of the .NET SDK toolchain "
                + "and not of Metalama. Verify the integrity of the .NET SDK installation, and consider excluding the build directories "
                + "from real-time antivirus scanning.";
        }

        return
            "The cause of this failure is generally the build environment, typically the .NET SDK installation or the NuGet configuration, "
            + "and not a defect of Metalama.";
    }

    /// <summary>
    /// Returns the sentence that quotes the errors that the nested build reported, folded into a single line because a
    /// Roslyn diagnostic cannot contain line breaks.
    /// </summary>
    public static string GetReportedErrors( ImmutableArray<string> output )
    {
        var errorLines = GetErrorLines( output );

        if ( errorLines.Count == 0 )
        {
            // Either the build failed before it could report a diagnostic, as when the .NET host cannot satisfy a
            // global.json, or the output was empty. The last lines are then the informative ones.
            errorLines = output
                .Select( Normalize )
                .Where( line => line.Length > 0 )
                .Reverse()
                .Take( _maxReportedErrorCount )
                .Reverse()
                .ToReadOnlyList();

            if ( errorLines.Count == 0 )
            {
                return "It did not produce any output.";
            }

            return "Its last output lines were the following: " + Truncate( string.Join( " ", errorLines ) );
        }

        return "It reported the following errors: " + Truncate( string.Join( " ", errorLines ) );
    }

    /// <summary>
    /// Returns the distinct error lines of the output, in the order in which they were produced.
    /// </summary>
    private static IReadOnlyList<string> GetErrorLines( ImmutableArray<string> output )
    {
        var errorLines = new List<string>();
        var seenErrorLines = new HashSet<string>( StringComparer.Ordinal );

        foreach ( var line in output )
        {
            if ( !_messageIdRegex.IsMatch( line ) && !_englishErrorRegex.IsMatch( line ) )
            {
                continue;
            }

            var normalizedLine = Normalize( line );

            // A multi-targeted build reports the same error once per target framework, and the suffix that names the
            // target framework is the only difference, so compare the lines after removing it.
            if ( normalizedLine.Length > 0 && seenErrorLines.Add( RemoveTargetFrameworkSuffix( normalizedLine ) ) )
            {
                errorLines.Add( normalizedLine );

                if ( errorLines.Count == _maxReportedErrorCount )
                {
                    break;
                }
            }
        }

        return errorLines;
    }

    /// <summary>
    /// Removes the <c>[project::TargetFramework=x]</c> suffix that MSBuild appends to a message of a multi-targeted build.
    /// </summary>
    private static string RemoveTargetFrameworkSuffix( string line )
    {
        var index = line.LastIndexOf( " [", StringComparison.Ordinal );

        return index > 0 && line.EndsWith( "]", StringComparison.Ordinal ) ? line.Substring( 0, index ) : line;
    }

    private static bool Contains( string text, string value ) => text.IndexOf( value, StringComparison.OrdinalIgnoreCase ) >= 0;

    /// <summary>
    /// Folds a line of the output into a form that can appear in a Roslyn diagnostic, which cannot contain line breaks.
    /// </summary>
    private static string Normalize( string line ) => _whitespaceRegex.Replace( line, " " ).Trim();

    private static string Truncate( string text )
        => text.Length <= _maxReportedErrorsLength ? text : text.Substring( 0, _maxReportedErrorsLength ) + " (truncated)";
}
