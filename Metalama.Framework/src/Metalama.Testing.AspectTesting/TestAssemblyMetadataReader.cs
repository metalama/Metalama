// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Reads the set of <see cref="TargetedAssemblyReference"/> from the project.
    /// </summary>
    internal sealed class TestAssemblyMetadataReader : ITestAssemblyMetadataReader
    {
        private static readonly ConcurrentDictionary<string, TestAssemblyMetadata> _projectOptionsCache = new();

        public TestAssemblyMetadata GetMetadata( IAssemblyInfo assembly )
        {
            return _projectOptionsCache.GetOrAdd( assembly.AssemblyPath, static ( _, a ) => GetMetadataCore( a ), assembly );
        }

        private static TestAssemblyMetadata GetMetadataCore( IAssemblyInfo assembly )
        {
            var projectDirectory = GetProjectDirectory();

            return new TestAssemblyMetadata(
                projectDirectory,
                GetSourceDirectory(),
                GetParserSymbols(),
                GetTargetFramework(),
                GetTargetFrameworks(),
                GetMustLaunchDebugger(),
                GetAssemblyReferences( "ReferenceAssemblyList" ),
                GetAssemblyReferences( "CompileTimeAssemblyList" ),
                GetAssemblyReferences( "ExtensionAssemblyList" ),
                ReadStringsFromFile( "PlugInList" ),
                GetGlobalUsingsFile(),
                GetIgnoredWarnings() );

            IAttributeInfo? GetOptionalAssemblyMetadataAttribute( string key )
                => assembly
                    .GetCustomAttributes( typeof(AssemblyMetadataAttribute) )
                    .SingleOrDefault( a => string.Equals( (string) a.GetConstructorArguments().First<object>(), key, StringComparison.Ordinal ) );

            IAttributeInfo GetRequiredAssemblyMetadataAttribute( string key )
                => GetOptionalAssemblyMetadataAttribute( key )
                   ?? throw new InvalidOperationException( $"The test assembly must have an AssemblyMetadataAttribute with Key = \"{key}\"." );

            string? GetOptionalAssemblyMetadataValue( string key )
                => (string?) GetOptionalAssemblyMetadataAttribute( key )?.GetConstructorArguments()?.ElementAt( 1 );

            string GetRequiredAssemblyMetadataValue( string key )
                => (string) (GetRequiredAssemblyMetadataAttribute( key ).GetConstructorArguments()?.ElementAt( 1 )
                             ?? throw new InvalidOperationException( "The AssemblyMetadataAttribute with Key = \"{key}\" contains no value." ));

            bool GetBoolAssemblyMetadataValue( string key )
            {
                var value = GetOptionalAssemblyMetadataValue( key );

                return !string.IsNullOrEmpty( value ) && value.ToLowerInvariant().Trim() == "true";
            }

            string GetProjectDirectory() => GetRequiredAssemblyMetadataValue( "ProjectDirectory" );

            string GetSourceDirectory() => GetOptionalAssemblyMetadataValue( "SourceDirectory" ) ?? GetProjectDirectory();

            ImmutableArray<string> GetParserSymbols()
                => (GetOptionalAssemblyMetadataValue( "DefineConstants" ) ?? "")
                    .Split( ';' )
                    .SelectAsReadOnlyList( s => s.Trim() )
                    .Where( s => !string.IsNullOrEmpty( s ) )
                    .ToImmutableArray();

            string GetTargetFramework() => GetRequiredAssemblyMetadataValue( "TargetFramework" );

            string? GetTargetFrameworks() => GetOptionalAssemblyMetadataValue( "TargetFrameworks" );

            ImmutableArray<TargetedAssemblyReference> GetAssemblyReferences( string propertyName )
            {
                var lines = ReadStringsFromFile( propertyName );

                // Issue #754: The test project's own output assembly should not be included
                // in the test compilation references. The MSBuild ReferencePathWithRefAssemblies
                // item group can include the test project assembly, which breaks isolation.
                var testAssemblyFileName = Path.GetFileName( assembly.AssemblyPath );

                return lines.SelectAsReadOnlyCollection(
                        t => TargetedAssemblyReference.ParsePipeSeparatedString( t, path => FindImplementationAssembly( projectDirectory, path ) ) )
                    .Where( t => t.SatisfiesCurrentProcess
                                 && !string.Equals( Path.GetFileName( t.Path ), testAssemblyFileName, StringComparison.OrdinalIgnoreCase ) )
                    .ToImmutableArray();
            }

            ImmutableArray<string> ReadStringsFromFile( string propertyName )
            {
                var path = GetRequiredAssemblyMetadataValue( propertyName );
                var lines = File.ReadAllLines( path );

                return lines.ToImmutableArray();
            }

            bool GetMustLaunchDebugger() => GetBoolAssemblyMetadataValue( "MetalamaDebugTestFramework" );

            string? GetGlobalUsingsFile() => GetOptionalAssemblyMetadataValue( "GlobalUsingsFile" );

            ImmutableArray<string> GetIgnoredWarnings()
                => GetOptionalAssemblyMetadataValue( "IgnoredWarnings" )
                    ?.Split( ';' )
                    .Select( s => s.Trim() )
                    .Where( s => !string.IsNullOrEmpty( s ) )
                    .ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }

        private static string FindImplementationAssembly( string projectDirectory, string path )
        {
            path = Path.Combine( projectDirectory, path );

            var directory = Path.GetDirectoryName( path )!;
            var leafDirectory = Path.GetFileName( directory );

            if ( leafDirectory.Equals( "ref", StringComparison.OrdinalIgnoreCase ) )
            {
                // If we get a reference assembly, take the implementation assembly instead.
                var implementationPath = Path.Combine( Path.GetDirectoryName( directory )!, Path.GetFileName( path ) );

                if ( File.Exists( implementationPath ) )
                {
                    return implementationPath;
                }
            }

            return path;
        }
    }
}