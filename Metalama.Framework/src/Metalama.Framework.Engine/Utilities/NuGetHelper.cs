// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Reads the <c>nuget.config</c> files that apply to a project, merges them into the configuration written beside the
/// reference-assembly project, and adds package sources to that copy.
/// </summary>
/// <remarks>
/// Every access to the file system and to the environment goes through <see cref="IFileSystem"/> and
/// <see cref="IEnvironmentVariableProvider"/>, so that the rules implemented here can be exercised by a test without
/// the machine that runs it taking part in the outcome.
/// </remarks>
internal sealed class NuGetHelper
{
    /// <summary>
    /// A package source of a <c>packageSourceMapping</c> section, with the patterns mapped to it.
    /// </summary>
    private readonly record struct MappedPackageSource( string Key, IReadOnlyList<string> Patterns );

    // Sections in nuget.config where the "value" attribute of <add> elements is a local path.
    private static readonly HashSet<string> _pathSections =
        new( StringComparer.OrdinalIgnoreCase ) { "fallbackPackageFolders" };

    // Sections where the "value" attribute may be either a URL or a local path.
    private static readonly HashSet<string> _mixedPathSections =
        new( StringComparer.OrdinalIgnoreCase ) { "packageSources" };

    // Keys in the <config> section whose values are local paths.
    private static readonly HashSet<string> _configPathKeys =
        new( StringComparer.OrdinalIgnoreCase ) { "repositoryPath", "globalPackagesFolder" };

    // A %NAME% token of a nuget.config value, which NuGet expands before it uses the value.
    private static readonly Regex _environmentVariableRegex = new( "%([^%]+)%", RegexOptions.CultureInvariant );

    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentVariableProvider _environmentVariables;

    public NuGetHelper( GlobalServiceProvider serviceProvider ) : this(
        serviceProvider.GetRequiredBackstageService<IFileSystem>(),
        serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>() ) { }

    public NuGetHelper( IFileSystem fileSystem, IEnvironmentVariableProvider environmentVariables )
    {
        this._fileSystem = fileSystem;
        this._environmentVariables = environmentVariables;
    }

    /// <summary>
    /// Returns the path of the user-level NuGet configuration file, or <c>null</c> when none exists.
    /// </summary>
    /// <remarks>
    /// NuGet reads this file for every project, including the reference-assembly project, because the file is not tied
    /// to a directory tree and is therefore not among the files returned by <see cref="GetConfigFiles"/>. It is read to
    /// decide whether a package source mapping section exists and whether a pattern is already mapped. It is not merged
    /// into the generated configuration. See issue #1885.
    /// </remarks>
    public string? GetUserConfigFile()
    {
        foreach ( var candidateDirectory in this.GetUserConfigDirectories() )
        {
            var configFile = this.FindConfigFileInDirectory( candidateDirectory );

            if ( configFile != null )
            {
                return configFile;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the directories in which NuGet looks for the user-level configuration file, in the order in which NuGet
    /// probes them.
    /// </summary>
    /// <remarks>
    /// The directories are formed from environment variables instead of from
    /// <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/>, so that a test decides what they are. The
    /// variables of both families of operating systems are read on every one of them, because a variable that the
    /// current one does not define yields no candidate.
    /// </remarks>
    private IEnumerable<string> GetUserConfigDirectories()
    {
        // The application data directory, which is %APPDATA% on Windows.
        var applicationData = this._environmentVariables.GetEnvironmentVariable( "APPDATA" );

        if ( !string.IsNullOrEmpty( applicationData ) )
        {
            yield return Path.Combine( applicationData!, "NuGet" );
        }

        // The same directory on Unix, which is $XDG_CONFIG_HOME when that variable is defined and $HOME/.config
        // otherwise.
        var xdgConfigHome = this._environmentVariables.GetEnvironmentVariable( "XDG_CONFIG_HOME" );
        var home = this._environmentVariables.GetEnvironmentVariable( "HOME" );

        if ( !string.IsNullOrEmpty( xdgConfigHome ) )
        {
            yield return Path.Combine( xdgConfigHome!, "NuGet" );
        }
        else if ( !string.IsNullOrEmpty( home ) )
        {
            yield return Path.Combine( home!, ".config", "NuGet" );
        }

        // The legacy location under the home directory of the user, which is %USERPROFILE% on Windows and $HOME on
        // Unix.
        var userProfile = this._environmentVariables.GetEnvironmentVariable( "USERPROFILE" );

        if ( !string.IsNullOrEmpty( userProfile ) )
        {
            yield return Path.Combine( userProfile!, ".nuget", "NuGet" );
        }

        if ( !string.IsNullOrEmpty( home ) )
        {
            yield return Path.Combine( home!, ".nuget", "NuGet" );
        }
    }

    /// <summary>
    /// Returns the path of the file named <c>nuget.config</c> in a given directory, whatever the case of its name, or
    /// <c>null</c> when the directory holds no such file or does not exist.
    /// </summary>
    /// <remarks>
    /// The directory is enumerated instead of a single name being probed, because the tools that create the file spell
    /// it <c>NuGet.Config</c> while NuGet itself matches the name without regard to case, and a file system that
    /// distinguishes case would otherwise hide the file.
    /// </remarks>
    private string? FindConfigFileInDirectory( string directory )
    {
        if ( !this._fileSystem.DirectoryExists( directory ) )
        {
            return null;
        }

        return this._fileSystem.EnumerateFiles( directory )
            .FirstOrDefault( f => string.Equals( Path.GetFileName( f ), "nuget.config", StringComparison.OrdinalIgnoreCase ) );
    }

    public List<string> GetConfigFiles( string projectPath )
    {
        List<string> configFiles = new();
        this.DiscoverConfigFiles( Path.GetDirectoryName( projectPath ).AssertNotNull(), configFiles );

        return configFiles;
    }

    private void DiscoverConfigFiles( string directory, List<string> configFiles )
    {
        var parentDirectory = Path.GetDirectoryName( directory );

        // Parent first.
        if ( parentDirectory != null )
        {
            this.DiscoverConfigFiles( parentDirectory, configFiles );
        }

        // Add one file.
        var path = Path.Combine( directory, "nuget.config" );

        if ( this._fileSystem.FileExists( path ) )
        {
            configFiles.Add( path );
        }
    }

    public XDocument? MergeConfigFiles( IReadOnlyList<string> configFiles )
    {
        if ( configFiles.Count == 0 )
        {
            return null;
        }

        var mergedDocument = new XDocument();
        mergedDocument.Add( new XElement( "configuration" ) );

        foreach ( var configFile in configFiles )
        {
            var document = this.LoadConfigFile( configFile );

            if ( document.Root == null )
            {
                continue;
            }

            var configDirectory = Path.GetDirectoryName( Path.GetFullPath( configFile ) ).AssertNotNull();

            this.ResolveRelativePaths( document.Root, configDirectory );

            MergeChildrenNodes( mergedDocument.Root!, document.Root );
        }

        return mergedDocument;
    }

    private XDocument LoadConfigFile( string configFile )
    {
        using var stream = this._fileSystem.OpenRead( configFile );

        return XDocument.Load( stream );
    }

    /// <summary>
    /// Adds a package source to a merged NuGet configuration, and maps a package pattern to it when the effective
    /// configuration uses package source mapping and does not already map that pattern.
    /// </summary>
    /// <param name="document">The merged configuration, modified in place.</param>
    /// <param name="key">The key under which the source is declared.</param>
    /// <param name="url">The address of the source.</param>
    /// <param name="packagePattern">The pattern mapped to the source, such as <c>Microsoft.CodeAnalysis.*</c>.</param>
    /// <param name="decisionConfigFiles">
    /// The configuration files that NuGet applies to the project without their being merged into
    /// <paramref name="document"/>, in the order in which NuGet applies them, that is, the user-level configuration
    /// file. They take part in the decision and are not reproduced in <paramref name="document"/>, except for the
    /// package source mapping entries that this method has to modify.
    /// </param>
    /// <remarks>
    /// <para>
    /// No mapping is written when the effective configuration declares no package source mapping, because writing one
    /// would activate mapping for every package, nor when the effective configuration already maps
    /// <paramref name="packagePattern"/> or a more specific pattern to another source, because that expresses an
    /// intention about these packages which this method does not override.
    /// </para>
    /// <para>
    /// When a mapping is written, every source that covers <paramref name="packagePattern"/> today through a shorter
    /// pattern receives <paramref name="packagePattern"/> as well. NuGet resolves a package identifier through the
    /// longest matching pattern and consults only the sources that declare that pattern, so mapping the pattern to the
    /// added source alone would make that source the only candidate, and the restore would fail on every matching
    /// package that the added source does not carry. Such a source is reproduced with every pattern it declares,
    /// because a <c>packageSource</c> element replaces the inherited element of the same key instead of adding to it.
    /// </para>
    /// <para>
    /// No file read by this method is modified. See issue #1885.
    /// </para>
    /// </remarks>
    public AddPackageSourceResult AddPackageSource(
        XDocument document,
        string key,
        string url,
        string packagePattern,
        IReadOnlyList<string> decisionConfigFiles )
    {
        var root = document.Root.AssertNotNull();

        var packageSources = GetOrAddSection( root, "packageSources" );

        var existingSource = packageSources.Elements( "add" )
            .FirstOrDefault( e => string.Equals( e.Attribute( "key" )?.Value, key, StringComparison.OrdinalIgnoreCase ) );

        if ( existingSource != null )
        {
            existingSource.SetAttributeValue( "value", url );
        }
        else
        {
            // The element is appended last so that it comes after any clear element, which removes every source
            // declared before it.
            packageSources.Add( new XElement( "add", new XAttribute( "key", key ), new XAttribute( "value", url ) ) );
        }

        var effectiveMapping = this.GetEffectivePackageSourceMapping( document, decisionConfigFiles );

        if ( effectiveMapping.Count == 0 )
        {
            return default;
        }

        var patternPrefix = GetPatternPrefix( packagePattern );

        foreach ( var mappedSource in effectiveMapping )
        {
            if ( string.Equals( mappedSource.Key, key, StringComparison.OrdinalIgnoreCase ) )
            {
                continue;
            }

            foreach ( var pattern in mappedSource.Patterns )
            {
                if ( GetPatternPrefix( pattern ).StartsWith( patternPrefix, StringComparison.OrdinalIgnoreCase ) )
                {
                    return new AddPackageSourceResult( false, mappedSource.Key, pattern );
                }
            }
        }

        var mappingSection = GetOrAddSection( root, "packageSourceMapping" );

        foreach ( var mappedSource in effectiveMapping )
        {
            if ( mappedSource.Patterns.Any( p => CoversThroughShorterPattern( p, patternPrefix ) ) )
            {
                SetMappedPatterns( mappingSection, mappedSource.Key, mappedSource.Patterns.Concat( new[] { packagePattern } ) );
            }
        }

        SetMappedPatterns( mappingSection, key, new[] { packagePattern } );

        return new AddPackageSourceResult( true, null, null );
    }

    /// <summary>
    /// Returns the package source mapping that NuGet applies to the project, that is, the mapping of
    /// <paramref name="document"/> applied on top of the mapping of <paramref name="decisionConfigFiles"/>.
    /// </summary>
    /// <remarks>
    /// A copy of the root element is merged, because <see cref="MergeChildrenNodes"/> moves the elements of the
    /// increment into the target and would otherwise empty the document that the caller is modifying.
    /// </remarks>
    private IReadOnlyList<MappedPackageSource> GetEffectivePackageSourceMapping(
        XDocument document,
        IReadOnlyList<string> decisionConfigFiles )
    {
        var root = document.Root.AssertNotNull();

        var effectiveRoot = this.MergeConfigFiles( decisionConfigFiles )?.Root;

        if ( effectiveRoot != null )
        {
            MergeChildrenNodes( effectiveRoot, new XElement( root ) );
        }
        else
        {
            effectiveRoot = root;
        }

        var mappingSection = effectiveRoot.Element( "packageSourceMapping" );

        if ( mappingSection == null )
        {
            return Array.Empty<MappedPackageSource>();
        }

        var mappedSources = new List<MappedPackageSource>();

        foreach ( var packageSource in mappingSection.Elements( "packageSource" ) )
        {
            var sourceKey = packageSource.Attribute( "key" )?.Value;

            if ( string.IsNullOrEmpty( sourceKey ) )
            {
                continue;
            }

            var patterns = packageSource.Elements( "package" )
                .Select( p => p.Attribute( "pattern" )?.Value )
                .Where( p => !string.IsNullOrEmpty( p ) )
                .Select( p => p! )
                .ToList();

            if ( patterns.Count > 0 )
            {
                mappedSources.Add( new MappedPackageSource( sourceKey!, patterns ) );
            }
        }

        return mappedSources;
    }

    /// <summary>
    /// Returns the literal part of a package source mapping pattern, that is, the pattern itself when it is a package
    /// identifier, or the part that precedes the wildcard when it ends with one.
    /// </summary>
    /// <remarks>
    /// A pattern is either a package identifier or a prefix followed by <c>*</c>, and the length of the literal part is
    /// what NuGet compares to determine which pattern is the longest match, so the literal part is what tells whether
    /// one pattern is more specific than another.
    /// </remarks>
    private static string GetPatternPrefix( string pattern )
        => pattern.EndsWith( "*", StringComparison.Ordinal ) ? pattern.Substring( 0, pattern.Length - 1 ) : pattern;

    /// <summary>
    /// Determines whether a pattern matches every package identifier matched by the pattern whose literal part is
    /// <paramref name="patternPrefix"/>, while being less specific than it.
    /// </summary>
    /// <remarks>
    /// Only a pattern that ends with a wildcard qualifies. A pattern that is a package identifier matches that
    /// identifier alone, so it covers no other package even when it is shorter, as <c>Microsoft.CodeAnalysis</c> is
    /// shorter than <c>Microsoft.CodeAnalysis.</c> and matches no package that the latter matches.
    /// </remarks>
    private static bool CoversThroughShorterPattern( string pattern, string patternPrefix )
    {
        if ( !pattern.EndsWith( "*", StringComparison.Ordinal ) )
        {
            return false;
        }

        var prefix = GetPatternPrefix( pattern );

        return prefix.Length < patternPrefix.Length && patternPrefix.StartsWith( prefix, StringComparison.OrdinalIgnoreCase );
    }

    /// <summary>
    /// Sets the patterns mapped to a package source in the given <c>packageSourceMapping</c> section, replacing the
    /// patterns of an existing element of the same key.
    /// </summary>
    private static void SetMappedPatterns( XElement mappingSection, string sourceKey, IEnumerable<string> patterns )
    {
        var packageElements = patterns
            .Select( p => new XElement( "package", new XAttribute( "pattern", p ) ) )
            .ToArray();

        var existing = mappingSection.Elements( "packageSource" )
            .FirstOrDefault( e => string.Equals( e.Attribute( "key" )?.Value, sourceKey, StringComparison.OrdinalIgnoreCase ) );

        if ( existing != null )
        {
            existing.RemoveNodes();
            existing.Add( packageElements );
        }
        else
        {
            mappingSection.Add( new XElement( "packageSource", new XAttribute( "key", sourceKey ), packageElements ) );
        }
    }

    private static XElement GetOrAddSection( XElement root, string name )
    {
        var section = root.Element( name );

        if ( section == null )
        {
            section = new XElement( name );
            root.Add( section );
        }

        return section;
    }

    private void ResolveRelativePaths( XElement root, string configDirectory )
    {
        foreach ( var section in root.Elements() )
        {
            var sectionName = section.Name.LocalName;

            if ( _pathSections.Contains( sectionName ) || _mixedPathSections.Contains( sectionName ) )
            {
                this.ResolvePathsInSection( section, configDirectory );
            }
            else if ( string.Equals( sectionName, "config", StringComparison.OrdinalIgnoreCase ) )
            {
                this.ResolvePathsInConfigSection( section, configDirectory );
            }
        }
    }

    /// <summary>
    /// Expands the <c>%NAME%</c> tokens of a value, leaving a token in place when the variable is not defined, which is
    /// what <see cref="Environment.ExpandEnvironmentVariables"/> does.
    /// </summary>
    private string ExpandEnvironmentVariables( string value )
        => _environmentVariableRegex.Replace(
            value,
            match => this._environmentVariables.GetEnvironmentVariable( match.Groups[1].Value ) ?? match.Value );

    private bool TryResolveRelativePath( string value, string configDirectory, out string resolvedPath )
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
        var expandedValue = this.ExpandEnvironmentVariables( value );

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

    private void ResolvePathsInSection( XElement section, string configDirectory )
    {
        foreach ( var element in section.Elements( "add" ) )
        {
            var valueAttribute = element.Attribute( "value" );

            if ( valueAttribute == null )
            {
                continue;
            }

            if ( this.TryResolveRelativePath( valueAttribute.Value, configDirectory, out var resolvedPath ) )
            {
                valueAttribute.Value = resolvedPath;
            }
        }
    }

    private void ResolvePathsInConfigSection( XElement configSection, string configDirectory )
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

            if ( this.TryResolveRelativePath( valueAttribute.Value, configDirectory, out var resolvedPath ) )
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
