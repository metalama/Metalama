// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Metalama.Framework.Engine.Utilities;

internal static class NuGetHelper
{
    // Sections in nuget.config where the "value" attribute of <add> elements is a local path.
    private static readonly HashSet<string> _pathSections =
        new( StringComparer.OrdinalIgnoreCase ) { "fallbackPackageFolders" };

    // Sections where the "value" attribute may be either a URL or a local path.
    private static readonly HashSet<string> _mixedPathSections =
        new( StringComparer.OrdinalIgnoreCase ) { "packageSources" };

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

            var configDirectory = Path.GetDirectoryName( Path.GetFullPath( configFile ) ).AssertNotNull();

            ResolveRelativePaths( document.Root, configDirectory );

            MergeChildrenNodes( mergedDocument.Root!, document.Root );
        }

        return mergedDocument;
    }

    private static void ResolveRelativePaths( XElement root, string configDirectory )
    {
        foreach ( var section in root.Elements() )
        {
            var sectionName = section.Name.LocalName;

            if ( _pathSections.Contains( sectionName ) )
            {
                ResolvePathsInSection( section, configDirectory, urlsAllowed: false );
            }
            else if ( _mixedPathSections.Contains( sectionName ) )
            {
                ResolvePathsInSection( section, configDirectory, urlsAllowed: true );
            }
        }
    }

    private static void ResolvePathsInSection( XElement section, string configDirectory, bool urlsAllowed )
    {
        foreach ( var element in section.Elements( "add" ) )
        {
            var valueAttribute = element.Attribute( "value" );

            if ( valueAttribute == null )
            {
                continue;
            }

            var value = valueAttribute.Value;

            if ( string.IsNullOrEmpty( value ) )
            {
                continue;
            }

            // Skip URLs.
            if ( urlsAllowed && Uri.TryCreate( value, UriKind.Absolute, out var uri ) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) )
            {
                continue;
            }

            // Skip absolute paths.
            if ( Path.IsPathRooted( value ) )
            {
                continue;
            }

            // Skip values containing environment variables (e.g. %PACKAGEHOME%).
            if ( value.IndexOf( "%", StringComparison.Ordinal ) >= 0 )
            {
                continue;
            }

            // Resolve the relative path against the config file's directory.
            valueAttribute.Value = Path.GetFullPath( Path.Combine( configDirectory, value ) );
        }
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
                foreach ( var targetElement in target.Elements().ToList() )
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