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

    // Keys in the <config> section whose values are local paths.
    private static readonly HashSet<string> _configPathKeys =
        new( StringComparer.OrdinalIgnoreCase ) { "repositoryPath", "globalPackagesFolder" };

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

            if ( _pathSections.Contains( sectionName ) || _mixedPathSections.Contains( sectionName ) )
            {
                ResolvePathsInSection( section, configDirectory );
            }
            else if ( string.Equals( sectionName, "config", StringComparison.OrdinalIgnoreCase ) )
            {
                ResolvePathsInConfigSection( section, configDirectory );
            }
        }
    }

    private static bool TryResolveRelativePath( string value, string configDirectory, out string resolvedPath )
    {
        resolvedPath = value;

        if ( string.IsNullOrEmpty( value ) )
        {
            return false;
        }

        // Skip absolute URIs (http, https, file, ftp, etc.).
        // On Windows, Uri.TryCreate parses "C:\foo" with scheme="c" (drive letter), so we
        // exclude single-letter schemes to avoid treating drive-letter paths as URIs.
        if ( Uri.TryCreate( value, UriKind.Absolute, out var uri ) && uri.Scheme.Length > 1 )
        {
            return false;
        }

        // Skip absolute paths.
        if ( Path.IsPathRooted( value ) )
        {
            return false;
        }

        // Handle environment variable references (%VAR%).
        // Expand to check whether the result is absolute. If the variable is undefined,
        // ExpandEnvironmentVariables leaves the %VAR% token as-is — we must not resolve it.
        var expandedValue = Environment.ExpandEnvironmentVariables( value );

        if ( expandedValue.IndexOf( "%", StringComparison.Ordinal ) >= 0 )
        {
            // The expanded value still contains '%', meaning at least one env var is undefined.
            // NuGet will use the literal value, so we should not resolve it.
            return false;
        }

        if ( !string.Equals( expandedValue, value, StringComparison.Ordinal ) )
        {
            // The value contained environment variables that were all resolved.
            // After expansion, the path may be absolute.
            if ( Path.IsPathRooted( expandedValue ) )
            {
                return false;
            }

            // Environment variable resolved to a relative path — resolve the expanded value.
            resolvedPath = Path.GetFullPath( Path.Combine( configDirectory, expandedValue ) );

            return true;
        }

        // Resolve the relative path against the config file's directory.
        resolvedPath = Path.GetFullPath( Path.Combine( configDirectory, value ) );

        return true;
    }

    private static void ResolvePathsInSection( XElement section, string configDirectory )
    {
        foreach ( var element in section.Elements( "add" ) )
        {
            var valueAttribute = element.Attribute( "value" );

            if ( valueAttribute == null )
            {
                continue;
            }

            if ( TryResolveRelativePath( valueAttribute.Value, configDirectory, out var resolvedPath ) )
            {
                valueAttribute.Value = resolvedPath;
            }
        }
    }

    private static void ResolvePathsInConfigSection( XElement configSection, string configDirectory )
    {
        foreach ( var element in configSection.Elements( "add" ) )
        {
            var keyAttribute = element.Attribute( "key" );
            var valueAttribute = element.Attribute( "value" );

            if ( keyAttribute == null || valueAttribute == null )
            {
                continue;
            }

            if ( !_configPathKeys.Contains( keyAttribute.Value ) )
            {
                continue;
            }

            if ( TryResolveRelativePath( valueAttribute.Value, configDirectory, out var resolvedPath ) )
            {
                valueAttribute.Value = resolvedPath;
            }
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
                var keyAttr = childIncrement.Attribute( "key" );

                if ( keyAttr != null )
                {
                    var existing = target.Elements( childIncrement.Name )
                        .FirstOrDefault( e => string.Equals( e.Attribute( "key" )?.Value, keyAttr.Value, StringComparison.OrdinalIgnoreCase ) );

                    if ( existing != null )
                    {
                        existing.ReplaceWith( childIncrement );
                    }
                    else
                    {
                        target.Add( childIncrement );
                    }
                }
                else
                {
                    target.Add( childIncrement );
                }
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