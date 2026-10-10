// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Options;

/// <summary>
/// Verifies that <c>Metalama.CompilerVisibleProperties.props</c> exports every MSBuild property that
/// <see cref="MSBuildProjectOptions"/> reads.
/// </summary>
/// <remarks>
/// An analyzer can read an MSBuild property only when the property is declared as a <c>CompilerVisibleProperty</c> item.
/// When a property is read but not exported, setting it in a project has no effect. See issue #2178.
/// </remarks>
public sealed class CompilerVisiblePropertiesTests
{
    /// <summary>
    /// The name of the assembly metadata item that carries the path of <c>Metalama.CompilerVisibleProperties.props</c>.
    /// </summary>
    private const string _compilerVisiblePropertiesFilePathKey = "CompilerVisiblePropertiesFilePath";

    /// <summary>
    /// The properties that <see cref="MSBuildProjectOptions"/> reads but that are exported to the compiler by
    /// Metalama.Compiler or by the .NET SDK, so that <c>Metalama.CompilerVisibleProperties.props</c> does not need to
    /// export them a second time.
    /// </summary>
    private static readonly HashSet<string> _propertiesExportedElsewhere = new( StringComparer.OrdinalIgnoreCase )
    {
        // Exported by Metalama.Compiler.props.
        "MetalamaCompilerTransformedFilesOutputPath",
        "MetalamaCompilerTransformerOrder",
        "MetalamaDebugCompiler",
        "MetalamaDebugTransformedCode",
        "MetalamaEmitCompilerTransformedFiles",
        "MetalamaLicense",
        "MetalamaTransformedCodeAnalyzers",
        "MSBuildProjectFullPath",
        "NETCoreSdkBundledVersionsProps"
    };

    [Fact]
    public void EveryPropertyReadByMSBuildProjectOptionsIsExported()
    {
        var source = new RecordingOptionsSource();
        var options = new TestableMSBuildProjectOptions( source );

        // Reads every property of the options object, so that the source records the name of every MSBuild property.
        for ( var type = options.GetType(); type != null && type != typeof(object); type = type.BaseType )
        {
            foreach ( var property in type.GetProperties(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ) )
            {
                if ( property.GetMethod == null || property.GetIndexParameters().Length > 0 )
                {
                    continue;
                }

                try
                {
                    property.GetValue( options );
                }
                catch ( TargetInvocationException )
                {
                    // A property that fails without a value has still recorded the names that it read.
                }
            }
        }

        Assert.Contains( MSBuildPropertyNames.MetalamaEnabled, source.ReadNames );

        var exportedProperties = ReadExportedProperties();

        var missingProperties = source.ReadNames
            .Where( name => !exportedProperties.Contains( name ) && !_propertiesExportedElsewhere.Contains( name ) )
            .ToOrderedList( name => name, StringComparer.Ordinal );

        Assert.Empty( missingProperties );
    }

    /// <summary>
    /// Reads the names of the <c>CompilerVisibleProperty</c> items of <c>Metalama.CompilerVisibleProperties.props</c>.
    /// </summary>
    private static HashSet<string> ReadExportedProperties()
    {
        var relativePath = typeof(CompilerVisiblePropertiesTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single( a => a.Key == _compilerVisiblePropertiesFilePathKey )
            .Value;

        Assert.NotNull( relativePath );

        var path = Path.GetFullPath( relativePath! );

        Assert.True( File.Exists( path ), $"The file '{path}' does not exist." );

        return XDocument.Load( path )
            .Descendants( "CompilerVisibleProperty" )
            .Select( e => e.Attribute( "Include" )?.Value.Trim() )
            .Where( name => !string.IsNullOrEmpty( name ) )
            .Select( name => name! )
            .ToHashSet( StringComparer.OrdinalIgnoreCase );
    }

    /// <summary>
    /// A <see cref="MSBuildProjectOptions"/> that can be instantiated by the test.
    /// </summary>
    private sealed class TestableMSBuildProjectOptions : MSBuildProjectOptions
    {
        public TestableMSBuildProjectOptions( IProjectOptionsSource source ) : base( source ) { }
    }

    /// <summary>
    /// An <see cref="IProjectOptionsSource"/> that has no value and records the name of every property that is read.
    /// </summary>
    private sealed class RecordingOptionsSource : IProjectOptionsSource
    {
        public HashSet<string> ReadNames { get; } = new( StringComparer.OrdinalIgnoreCase );

        public bool TryGetValue( string name, out string? value )
        {
            this.ReadNames.Add( name );
            value = null;

            return false;
        }

        public IEnumerable<string> PropertyNames => [];
    }
}
