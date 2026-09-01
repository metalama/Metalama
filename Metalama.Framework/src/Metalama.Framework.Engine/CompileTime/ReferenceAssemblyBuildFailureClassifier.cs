// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Utilities;
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
/// The child process writes its output in the language of the toolchain, and a substantial share of the affected users
/// run a localized toolchain, therefore no rule of this class may depend on the wording of a message. Only the
/// following signals are recognized, all of which are invariant across languages: message identifiers such as
/// <c>NU1101</c>, process exit codes, HTTP status codes, and the file name <c>global.json</c>. A condition for which no
/// invariant signal exists is deliberately left unclassified and falls back to the generic explanation, which is a less
/// precise message but never a wrong one.
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
    /// The maximal number of lines quoted by <see cref="GetReportedErrors"/>. A multi-targeted build repeats the same
    /// error once per target framework, so quoting all of them adds length but no information.
    /// </summary>
    private const int _maxReportedErrorCount = 4;

    /// <summary>
    /// Matches a line that carries an MSBuild, NuGet, .NET SDK or compiler message identifier followed by a colon, such
    /// as <c>error NU1101:</c>.
    /// </summary>
    /// <remarks>
    /// The identifier is the only part of such a line that is invariant across languages: both the word that precedes it
    /// and the text that follows it are localized.
    /// </remarks>
    private static readonly Regex _messageIdRegex = new(
        @"\b(?:NU|NETSDK|MSB|CS|AD)[0-9]{3,5}\s*:",
        RegexOptions.CultureInvariant );

    /// <summary>
    /// Matches an HTTP status code that denotes an authentication or authorization failure, as it appears in the message
    /// that NuGet produces for a rejected request.
    /// </summary>
    /// <remarks>
    /// The status code is a number defined by the HTTP protocol and is therefore invariant, whereas the sentence that
    /// carries it is localized. The boundaries exclude a digit or a dot on either side so that a version such as
    /// <c>1.0.401</c> does not match.
    /// </remarks>
    private static readonly Regex _unauthorizedStatusCodeRegex = new(
        @"(?<![\w.])40[13](?![\w.])",
        RegexOptions.CultureInvariant );

    /// <summary>
    /// Matches a run of whitespace, used to fold the output into the single line that a Roslyn diagnostic requires.
    /// </summary>
    private static readonly Regex _whitespaceRegex = new( @"\s+", RegexOptions.CultureInvariant );

    /// <summary>
    /// The literal part of <see cref="SupportedCSharpVersions.RoslynPackagePattern"/>, that is, the prefix of the
    /// identifier of every Roslyn package that the reference-assembly project requests.
    /// </summary>
    private static readonly string _roslynPackagePrefix = SupportedCSharpVersions.RoslynPackagePattern.TrimEnd( '*' );

    /// <summary>
    /// Returns the sentence that describes the probable cause of the failure and the action that resolves it, or the
    /// generic sentence when the output carries no signal that identifies a known condition.
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
    /// <param name="unmappedPrereleasePackageSource">
    /// The address of the package source that Metalama declared for the prerelease Roslyn packages without a package
    /// source mapping, because the configuration already maps these packages to another source, or <c>null</c> when
    /// Metalama declared no such source or mapped it. See issue #1885.
    /// </param>
    /// <remarks>
    /// The rules are ordered by decreasing confidence: a message identifier identifies a condition exactly, whereas the
    /// presence of an HTTP status code or of a file name is circumstantial, so the former must win when both are present.
    /// </remarks>
    public static string GetProbableCause(
        ImmutableArray<string> output,
        string projectDirectory,
        string? requestedSdkVersion,
        string? unmappedPrereleasePackageSource = null )
    {
        var text = string.Join( "\n", output );

        if ( ContainsMessageId( text, "NU1302" ) )
        {
            return
                "A NuGet feed is configured with an insecure HTTP address, which NuGet rejects by default. Give that feed an HTTPS address, "
                + "or set the allowInsecureConnections attribute of that feed in nuget.config.";
        }

        // packageSourceMapping applies to the nested build because it inherits the nuget.config of the project, and a
        // Microsoft.CodeAnalysis.* pattern is more specific than a * pattern. See issue #1747.
        if ( ContainsMessageId( text, "NU1101" ) )
        {
            // This build of Metalama requires Roslyn packages that nuget.org does not serve, and the mapping that would
            // have made them resolvable was skipped silently because the user had already mapped these packages. The
            // failure of the restore is the only point at which that decision becomes visible. See issue #1885. The
            // temporary project also references the compile-time packages of the user project, so this cause applies
            // only when the package that was not found is one of the Roslyn packages.
            if ( unmappedPrereleasePackageSource != null && ContainsUnresolvedRoslynPackage( output ) )
            {
                return
                    "A package that is required to resolve the compile-time reference assemblies was not found on the configured NuGet sources. "
                    + $"This build of Metalama requires Roslyn packages that are served by '{unmappedPrereleasePackageSource}' and not by nuget.org. "
                    + "That source was added to the NuGet configuration of this build, but no package source mapping was added for it, because "
                    + "nuget.config already maps the Microsoft.CodeAnalysis.* pattern, or a more specific one, to another source. Map the "
                    + "Microsoft.CodeAnalysis.* pattern to that source as well.";
            }

            return
                "A package that is required to resolve the compile-time reference assemblies was not found on the configured NuGet sources. "
                + "When nuget.config maps the Microsoft.CodeAnalysis.* pattern to a private feed through packageSourceMapping, this build is "
                + "restricted to that feed, therefore these packages must be mapped to a source that provides them, such as nuget.org.";
        }

        if ( ContainsMessageId( text, "NETSDK1045" ) )
        {
            return
                "The .NET SDK that this build resolved is too old for the target frameworks of the reference-assembly project. "
                + $"Install a more recent .NET SDK, or set the {MSBuildPropertyNames.MetalamaCompileTimeTargetFrameworks} MSBuild property "
                + "to target frameworks that this .NET SDK supports.";
        }

        // 0xC0000005, that is, STATUS_ACCESS_VIOLATION, reported by MSB6006 as the exit code of a task that crashed. Both
        // the identifier and the exit code are invariant. See issue #1746.
        if ( ContainsMessageId( text, "MSB6006" ) && text.IndexOf( "-1073741819", StringComparison.Ordinal ) >= 0 )
        {
            return
                "A tool started by this build terminated abnormally with an access violation. This is a failure of the .NET SDK toolchain "
                + "and not of Metalama. Verify the integrity of the .NET SDK installation, and consider excluding the build directories "
                + "from real-time antivirus scanning.";
        }

        // The nested build is a separate process and therefore does not inherit the interactive credential provider of
        // the IDE or of the outer build. See issue #1744. NuGet reports the transport failure without a message
        // identifier on some paths, so the HTTP status code is the only invariant signal available here. It is accepted
        // only in an output that also contains a URL, so that an unrelated occurrence of the number does not match.
        if ( _unauthorizedStatusCodeRegex.IsMatch( text ) && text.IndexOf( "http", StringComparison.OrdinalIgnoreCase ) >= 0 )
        {
            return
                "A NuGet feed refused the request with an HTTP authentication error. This build runs in a separate process, "
                + "which does not inherit the interactive credential provider of the outer build, therefore the credentials of the feed "
                + "must be available without user interaction, typically in a nuget.config file or through a credential provider.";
        }

        if ( ContainsMessageId( text, "NU1301" ) )
        {
            return
                "A NuGet feed could not be reached. Verify that the feed is available from this computer and that the "
                + "network configuration of the build environment allows the build to reach it.";
        }

        // When the .NET host cannot satisfy the SDK version that a global.json requests, it fails before MSBuild is
        // loaded, so the output carries no message identifier at all. The file name is the only invariant signal, which
        // is why this rule comes after every rule based on an identifier. See issue #1745.
        if ( text.IndexOf( "global.json", StringComparison.OrdinalIgnoreCase ) >= 0 )
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

        return
            "The cause of this failure is generally the build environment, typically the .NET SDK installation or the NuGet configuration, "
            + "and not a defect of Metalama.";
    }

    /// <summary>
    /// Returns the sentence that quotes the lines of the output that explain the failure, folded into a single line
    /// because a Roslyn diagnostic cannot contain line breaks.
    /// </summary>
    /// <remarks>
    /// The lines that carry a message identifier are preferred, because they are the diagnostics of the child build and
    /// can be recognized whatever the language of the toolchain. When the output carries none, as when the .NET host
    /// fails before MSBuild is loaded, the last lines are quoted instead: they cannot be recognized as errors without
    /// depending on the language, but they are where a failing build explains itself.
    /// </remarks>
    public static string GetReportedErrors( ImmutableArray<string> output )
    {
        var errorLines = GetLinesWithMessageId( output );

        if ( errorLines.Count > 0 )
        {
            return "It reported the following errors: " + Truncate( string.Join( " ", errorLines ) );
        }

        var lastLines = output
            .Select( Normalize )
            .Where( line => line.Length > 0 )
            .Reverse()
            .Take( _maxReportedErrorCount )
            .Reverse()
            .ToReadOnlyList();

        if ( lastLines.Count == 0 )
        {
            return "It did not produce any output.";
        }

        return "Its last output lines were the following: " + Truncate( string.Join( " ", lastLines ) );
    }

    /// <summary>
    /// Returns the distinct lines of the output that carry a message identifier, in the order in which they were produced.
    /// </summary>
    private static IReadOnlyList<string> GetLinesWithMessageId( ImmutableArray<string> output )
    {
        var errorLines = new List<string>();
        var seenErrorLines = new HashSet<string>( StringComparer.Ordinal );

        foreach ( var line in output )
        {
            if ( !_messageIdRegex.IsMatch( line ) )
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

    /// <summary>
    /// Determines whether a line that reports an unresolved package names a Roslyn package.
    /// </summary>
    /// <remarks>
    /// A package identifier is invariant across languages, as a message identifier is, so this test holds in a
    /// localized toolchain. The identifier is looked for in the lines that carry <c>NU1101</c> and in no other, so that
    /// an occurrence of the prefix elsewhere in the output, such as in the name of a reference of the project, does not
    /// match.
    /// </remarks>
    private static bool ContainsUnresolvedRoslynPackage( ImmutableArray<string> output )
        => output.Any(
            line => ContainsMessageId( line, "NU1101" )
                    && line.IndexOf( _roslynPackagePrefix, StringComparison.OrdinalIgnoreCase ) >= 0 );

    /// <summary>
    /// Determines whether the output contains the given message identifier, which must be followed by a colon so that an
    /// occurrence in prose, such as a suggestion to consult the documentation of that message, does not match.
    /// </summary>
    private static bool ContainsMessageId( string text, string messageId )
    {
        var index = 0;

        while ( (index = text.IndexOf( messageId, index, StringComparison.OrdinalIgnoreCase )) >= 0 )
        {
            var end = index + messageId.Length;

            // Skip the whitespace that some loggers insert between the identifier and the colon. Any whitespace is
            // skipped, and not the space alone, so that this method recognizes exactly what _messageIdRegex does.
            while ( end < text.Length && char.IsWhiteSpace( text[end] ) )
            {
                end++;
            }

            if ( end < text.Length && text[end] == ':' )
            {
                return true;
            }

            index += messageId.Length;
        }

        return false;
    }

    /// <summary>
    /// Folds a line of the output into a form that can appear in a Roslyn diagnostic, which cannot contain line breaks.
    /// </summary>
    private static string Normalize( string line ) => _whitespaceRegex.Replace( line, " " ).Trim();

    private static string Truncate( string text )
        => text.Length <= _maxReportedErrorsLength ? text : text.Substring( 0, _maxReportedErrorsLength ) + " (truncated)";
}
