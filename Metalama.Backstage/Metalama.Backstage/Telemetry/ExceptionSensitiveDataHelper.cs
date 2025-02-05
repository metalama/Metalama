// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Metalama.Backstage.Telemetry
{
    internal class ExceptionSensitiveDataHelper
    {
        // The Windows regex takes all words delimited by space after the path.
        private const string _windowsPathRegex = @"(?:[a-zA-Z]\:)?\\[^\:;\r\n"",'\]\}]+";

        // The Linux regex doesn't take words delimited by space after the path, but requires the path to have escaped spaces.
        private const string _unixPathRegex = @"/(?:(?:[^\:;\r\n"",'\]\}\ ])|(?:(?<=\\)\ ))+";

        public static readonly ExceptionSensitiveDataHelper Instance = new();

        private static readonly Regex _userNameRegEx =
            new(
                @"(?<![\.\^0-9a-zA-Z<>_`])(?![0-9]|Microsoft\.|MS\.|System\.|PostSharp\.|Metalama\.|Presentation|EnvDTE|Windows|`)[a-zA-Z0-9\$`@_\?]+(?:\.(?![0-9])\.?[a-zA-Z0-9\$`@<>_]+)+(?![\.\^0-9a-zA-Z`@_\$])" );

        private readonly Regex _pathRegex;

        internal ExceptionSensitiveDataHelper( bool? isWindows = null )
        {
            if ( isWindows == null )
            {
                isWindows = RuntimeInformation.IsOSPlatform( OSPlatform.Windows );
            }

            this._pathRegex = new Regex( isWindows.Value ? _windowsPathRegex : _unixPathRegex );
        }

        /// <exclude />
        public string RemoveSensitiveData( string? input )
        {
            if ( input == null )
            {
                return "";
            }

            return this._pathRegex.Replace( _userNameRegEx.Replace( input, "#user" ), "#path" );
        }
    }
}