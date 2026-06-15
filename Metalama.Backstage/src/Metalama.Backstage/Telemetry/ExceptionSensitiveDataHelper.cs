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

        private readonly Regex? _pathRegex;
        private readonly bool _enabled;

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

            return this._pathRegex!.Replace( _userNameRegEx.Replace( input, "#user" ), "#path" );
        }
    }
}