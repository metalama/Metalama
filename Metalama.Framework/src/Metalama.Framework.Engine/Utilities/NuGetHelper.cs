// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Metalama.Framework.Engine.Utilities;

internal static class NuGetHelper
{
    public static List<string> GetConfigFiles( string projectPath )
    {
        List<string> configFiles = new();
        DiscoverConfigFiles( Path.GetDirectoryName( projectPath ).AssertNotNull() );

        void DiscoverConfigFiles( string directory )
        {
            var parentDirectory = Path.GetDirectoryName( directory );

            // Parent first.
            if ( parentDirectory != null )
            {
                DiscoverConfigFiles( parentDirectory );
            }

            // Add one file.
            var path = Path.Combine( directory, "nuget.config" );

            if ( File.Exists( path ) )
            {
                configFiles.Add( path );
            }
        }

        return configFiles;
    }

    public static XDocument? MergeConfigFiles( IReadOnlyList<string> configFiles )
    {
        if ( configFiles.Count == 0 )
        {
            return null;
        }

        var mergedDocument = new XDocument();
        mergedDocument.Add( new XElement( "configuration" ) );

        foreach ( var configFile in configFiles )
        {
            var document = XDocument.Load( configFile );

            if ( document.Root == null )
            {
                continue;
            }

            MergeChildrenNodes( mergedDocument.Root!, document.Root );
        }

        return mergedDocument;
    }

    private static void MergeChildrenNodes( XElement target, XElement increment )
    {
        // This is a trivial algorithm to merge nuget.config without looking semantically at the file,
        // except for the <clear/> element. The logic is to merge any element that has no attribute, and to
        // add any element that has attributes. This seems to work for everything in nuget.config.
        foreach ( var childIncrement in increment.Elements() )
        {
            if ( childIncrement.Name == "clear" )
            {
                foreach ( var targetElement in target.Elements() )
                {
                    targetElement.Remove();
                }

                // Make sure we also clear system-wide configurations.
                target.Add( childIncrement );
            }
            else if ( childIncrement.HasAttributes )
            {
                target.Add( childIncrement );
            }
            else
            {
                var existingTargetChild = target.Elements( childIncrement.Name ).SingleOrDefault();

                if ( existingTargetChild != null )
                {
                    MergeChildrenNodes( existingTargetChild, childIncrement );
                }
                else
                {
                    target.Add( childIncrement );
                }
            }
        }
    }
}