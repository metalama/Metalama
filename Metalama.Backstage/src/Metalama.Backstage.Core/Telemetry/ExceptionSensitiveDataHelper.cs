// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Metalama.Backstage.Telemetry
{
    internal sealed class ExceptionSensitiveDataHelper
    {
        // The Windows regex takes all words delimited by space after the path.
        private const string _windowsPathRegex = @"(?:[a-zA-Z]\:)?\\[^\:;\r\n"",'\]\}]+";

        // The Linux regex doesn't take words delimited by space after the path, but requires the path to have escaped spaces.
        private const string _unixPathRegex = @"/(?:(?:[^\:;\r\n"",'\]\}\ ])|(?:(?<=\\)\ ))+";

        // First-party, framework and well-known bundled-OSS prefixes that are safe to disclose in an exception report
        // (their names cannot identify the user's product). These are matched at a name boundary (exact, or followed by
        // a non-letter), so a user namespace that merely starts with the same letters — e.g. "SystemwideTool" — is still
        // redacted. Together with _knownSafePrefixFamilies below, this is the single source of truth shared by the
        // namespace scrubber (_userNameRegEx) and the assembly classifier (IsKnownSafePrefix). See #1680.
        private static readonly string[] _frameworkSafePrefixes =
        {
            "System", "Microsoft", "MS", "Spectre", "mscorlib", "netstandard",
            "JetBrains", "Newtonsoft", "MessagePack", "StreamJsonRpc", "Nerdbank", "Mono", "xunit", "testhost", "Roslyn"
        };

        // The framework prefixes above followed by the prefixes of the vendor of the product, which come from the
        // product profile.
        private readonly string[] _knownSafePrefixes;

        // Framework assembly families whose members extend the prefix without a separator — e.g. PresentationFramework /
        // PresentationCore / PresentationUI, WindowsBase / Windows.UI.*, EnvDTE / EnvDTE80 / EnvDTE90. These are matched
        // as open prefixes (any name starting with them is safe), so the whole family is disclosed. See #1680.
        private static readonly string[] _knownSafePrefixFamilies = { "Presentation", "Windows", "EnvDTE" };

        // The static instances below are declared after the arrays above, because static field initializers run in
        // textual order and the constructor reads those arrays.

        /// <summary>
        /// The scrubber that trusts the framework prefixes only, and therefore redacts the namespaces of every vendor.
        /// It serves the callers that have no product profile, such as <see cref="DefaultExceptionAdapter"/>; the
        /// services obtain their scrubber from the profile with <see cref="ForProfile"/>.
        /// </summary>
        public static readonly ExceptionSensitiveDataHelper WithoutTrustedPrefixes = new( ImmutableArray<string>.Empty );

        // A dotted identifier (e.g. a namespace-qualified type in a stack trace) is redacted to "#user" unless it is on
        // the safe list. The negative-lookahead allow-list is built from the same two arrays as IsKnownSafePrefix:
        // boundary-matched roots (with a trailing (?![A-Za-z])) and open-matched framework families.
        private readonly Regex? _userNameRegEx;

        // An identity scrubber that leaves the input unchanged. It is used to render the full, unscrubbed local report
        // shown side-by-side with the scrubbed upload payload on the review page, so the user can see exactly what the
        // scrubber removes before anything leaves the machine. See #1674.
        public static readonly ExceptionSensitiveDataHelper Disabled = new( ImmutableArray<string>.Empty, enabled: false );

        // Redacts the value following an HTTP "Bearer" authentication scheme (e.g. "Bearer eyJ...").
        // These tokens are non-dotted and would otherwise pass through the heuristic above. See #1680.
        private static readonly Regex _bearerTokenRegEx =
            new( @"(?<scheme>\bBearer\s+)(?<value>[A-Za-z0-9\._\-\+/]+=*)", RegexOptions.IgnoreCase );

        // Denylist of secret-like "key=value" / "key: value" shapes (passwords, tokens, API keys,
        // connection-string segments, usernames). The value is redacted regardless of whether it is
        // dotted, so secrets that the dotted-identifier heuristic misses do not leave the machine. See #1680.
        private static readonly Regex _secretKeyValueRegEx =
            new(
                @"(?<key>[\w\-]*(?:password|passwd|pwd|secret|token|api[_\-]?key|access[_\-]?key|client[_\-]?secret|credentials?|signature|user(?:name|id)?|uid)[\w\-]*)(?<sep>\s*[:=]\s*)(?<value>[^\s;,""'\]\}]+)",
                RegexOptions.IgnoreCase );

        private readonly Regex? _pathRegex;
        private readonly bool _enabled;

        /// <summary>
        /// Gets a value indicating whether this instance scrubs its input. <c>false</c> for the <see cref="Disabled"/>
        /// identity scrubber used to render the full local report, which lets callers include content that is withheld
        /// from the scrubbed upload payload (such as <c>Exception.Message</c> and <c>Exception.Data</c>). See #1674, #1680.
        /// </summary>
        public bool IsEnabled => this._enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionSensitiveDataHelper"/> class.
        /// </summary>
        /// <param name="trustedPrefixes">The prefixes of the assemblies and namespaces of the vendor, which are disclosed in addition to the framework ones. See <see cref="ProductProfile.TrustedAssemblyNamePrefixes"/>.</param>
        /// <param name="isWindows">Whether the paths are Windows paths, or <c>null</c> to detect the current platform.</param>
        /// <param name="enabled">Whether the scrubber redacts its input at all.</param>
        internal ExceptionSensitiveDataHelper( ImmutableArray<string> trustedPrefixes, bool? isWindows = null, bool enabled = true )
        {
            this._enabled = enabled;
            this._knownSafePrefixes = _frameworkSafePrefixes.Concat( trustedPrefixes ).ToArray();

            if ( enabled )
            {
                isWindows ??= RuntimeInformation.IsOSPlatform( OSPlatform.Windows );

                this._pathRegex = new Regex( isWindows.Value ? _windowsPathRegex : _unixPathRegex );

                // A dotted identifier (e.g. a namespace-qualified type in a stack trace) is redacted to "#user" unless it is
                // on the safe list. The negative-lookahead allow-list is built from the same two arrays as IsKnownSafePrefix:
                // boundary-matched roots (with a trailing (?![A-Za-z])) and open-matched framework families.
                this._userNameRegEx =
                    new Regex(
                        @"(?<![\.\^0-9a-zA-Z<>_`])(?![0-9]|(?:" + string.Join( "|", this._knownSafePrefixes ) + @")(?![A-Za-z])|(?:"
                        + string.Join( "|", _knownSafePrefixFamilies ) + @")|`)[a-zA-Z0-9\$`@_\?]+(?:\.(?![0-9])\.?[a-zA-Z0-9\$`@<>_]+)+(?![\.\^0-9a-zA-Z`@_\$])" );
            }
        }

        /// <summary>
        /// Creates the scrubber of a product, which discloses the assemblies and namespaces of the vendor of that
        /// product.
        /// </summary>
        public static ExceptionSensitiveDataHelper ForProfile( ProductProfile profile ) => new( profile.TrustedAssemblyNamePrefixes );

        /// <exclude />
        public string RemoveSensitiveData( string? input )
        {
            if ( input == null )
            {
                return "";
            }

            if ( !this._enabled )
            {
                return input;
            }

            // Redact secret-like values first, so the resulting "#secret" markers are not themselves
            // mistaken for user identifiers or paths by the heuristics below.
            var withoutSecrets = _secretKeyValueRegEx.Replace( _bearerTokenRegEx.Replace( input, "${scheme}#secret" ), "${key}${sep}#secret" );

            return this._pathRegex!.Replace( this._userNameRegEx!.Replace( withoutSecrets, "#user" ), "#path" );
        }

        // Returns true if the assembly or namespace <paramref name="name"/> starts with one of <see cref="KnownSafePrefixes"/>
        // at a name boundary: the name is exactly the prefix, or the next character is not a letter (so "EnvDTE80" and
        // "System.Private.CoreLib" match, but a user assembly such as "SystemwideTool" does not). This is the assembly-list
        // counterpart of the namespace scrubber's allow-list, sharing the same prefix list and boundary rule. See #1680.
        internal bool IsKnownSafePrefix( string? name )
        {
            if ( string.IsNullOrEmpty( name ) )
            {
                return false;
            }

            foreach ( var prefix in this._knownSafePrefixes )
            {
                // Boundary match: the name is exactly the prefix, or the next character is not a letter (so "System.X"
                // passes but "SystemwideTool" does not).
                if ( name!.StartsWith( prefix, StringComparison.OrdinalIgnoreCase )
                     && ( name.Length == prefix.Length || !char.IsLetter( name[prefix.Length] ) ) )
                {
                    return true;
                }
            }

            foreach ( var family in _knownSafePrefixFamilies )
            {
                // Open prefix match: the whole family (e.g. PresentationFramework, WindowsBase, EnvDTE80) is disclosed.
                if ( name!.StartsWith( family, StringComparison.OrdinalIgnoreCase ) )
                {
                    return true;
                }
            }

            return false;
        }
    }
}