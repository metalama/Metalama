// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Orders instances of <see cref="FacadeProperty"/>.
    /// </summary>
    internal sealed class PropertyComparer : IComparer<(string Name, Type Type)>
    {
        private PropertyComparer() { }

        public static readonly PropertyComparer Instance = new();

        private static int GetPropertyNamePriority( string s )
            => s switch
            {
                "Index" => 0,
                "Id" => 0,
                "Severity" => 1,
                "Position" => 1,
                "ShortName" => 2,
                "Name" => 3,
                "DisplayName" => 3,
                "FullName" => 4,

                _ => 10
            };

        private static int GetPropertyTypePriority( Type t ) => t.GetInterface( "IEnumerable`1" ) != null ? 1 : 0;

        public int Compare( (string Name, Type Type) x, (string Name, Type Type) y )
        {
            var priorityComparison = GetPropertyNamePriority( x.Name ).CompareTo( GetPropertyNamePriority( y.Name ) );

            if ( priorityComparison != 0 )
            {
                return priorityComparison;
            }

            var typeComparison = GetPropertyTypePriority( x.Type ).CompareTo( GetPropertyTypePriority( y.Type ) );

            if ( typeComparison != 0 )
            {
                return typeComparison;
            }

            return StringComparer.Ordinal.Compare( x.Name, y.Name );
        }
    }
}