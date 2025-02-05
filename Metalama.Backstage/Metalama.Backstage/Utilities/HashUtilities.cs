// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Text;

namespace Metalama.Backstage.Utilities
{
    internal static class HashUtilities
    {
        public static string HashString( string s ) => HexHelper.FormatBytes( new MD5Managed().ComputeHash( Encoding.UTF8.GetBytes( s ) ) );
    }
}