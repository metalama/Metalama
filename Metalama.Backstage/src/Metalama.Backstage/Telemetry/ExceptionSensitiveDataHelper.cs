// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        public static readonly ExceptionSensitiveDataHelper Instance = new();

        private static readonly Regex _userNameRegEx =
            new(
                @"(?<![\.\^0-9a-zA-Z<>_`])(?![0-9]|Microsoft\.|MS\.|System\.|PostSharp\.|Metalama\.|Presentation|EnvDTE|Windows|`)[a-zA-Z0-9\$`@_\?]+(?:\.(?![0-9])\.?[a-zA-Z0-9\$`@<>_]+)+(?![\.\^0-9a-zA-Z`@_\$])" );

        // An identity scrubber that leaves the input unchanged. It is used to render the full, unscrubbed local report
        // shown side-by-side with the scrubbed upload payload on the review page, so the user can see exactly what the
        // scrubber removes before anything leaves the machine. See #1674.
        public static readonly ExceptionSensitiveDataHelper Disabled = new( enabled: false );

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

        internal ExceptionSensitiveDataHelper( bool? isWindows = null, bool enabled = true )
        {
            this._enabled = enabled;

            if ( enabled )
            {
                if ( isWindows == null )
                {
                    isWindows = RuntimeInformation.IsOSPlatform( OSPlatform.Windows );
                }

                this._pathRegex = new Regex( isWindows.Value ? _windowsPathRegex : _unixPathRegex );
            }
        }

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

            return this._pathRegex!.Replace( _userNameRegEx.Replace( withoutSecrets, "#user" ), "#path" );
        }
    }
}