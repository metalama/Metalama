// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Aspects.Introspection.Query;
using SharpCrafters.Aspects.Introspection.Query.Projects;
using SharpCrafters.Aspects.Introspection.Query.V2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Metalama.Tool.Introspection;

/// <summary>
/// The provider of Metalama for the <c>dotnet metalama introspect</c> commands. It reads the introspection file (<c>.mlsym</c>,
/// schema version 2) that the compile-time pipeline writes when the <c>MetalamaWriteIntrospectionFile</c> MSBuild property is
/// <c>True</c>.
/// </summary>
internal sealed class MetalamaIntrospectionProvider : IntrospectionProvider
{
    /// <summary>
    /// The fields that the file of Metalama does not record, as <c>type.field</c>. They exist for the files of PostSharp.
    /// </summary>
    private static readonly HashSet<string> _unprovidedFields = new( StringComparer.Ordinal )
    {
        "aspectClass.hasLimitedLicense",
        "aspectInstance.tags",
        "aspectInstance.linesOfCodeAvoided",
        "transformation.semantic",
        "transformation.adviceDeclaration",
        "transformation.linesOfCodeAvoided"
    };

    /// <summary>
    /// The collections that the file of Metalama does not record.
    /// </summary>
    private static readonly HashSet<string> _unprovidedTypes = new( StringComparer.Ordinal ) { "annotation", "declarationMapping" };

    public override string ProductName => "Metalama";

    public override string FileProperty => "MetalamaIntrospectionFile";

    public override string FileExtension => ".mlsym";

    public override string MissingFileHint
        => "Build the project with the MSBuild property MetalamaWriteIntrospectionFile set to True, for instance with "
           + "`dotnet build -p:MetalamaWriteIntrospectionFile=True`, or by setting the property in Directory.Build.props. The command never builds.";

    public override IntrospectionModel Load( IntrospectionTarget target )
    {
        using var stream = File.OpenRead( target.FilePath );

        return IntrospectionV2Loader.Load( stream, target.AssemblyName, target.Project.Directory );
    }

    public override bool Provides( string typeName, string? fieldName )
        => !_unprovidedTypes.Contains( typeName ) && (fieldName == null || !_unprovidedFields.Contains( typeName + "." + fieldName ));

    public override IIntrospectionSourceLocations? GetSourceLocations( IntrospectionTarget target )
        => target.Exists ? new SourceLocations( target ) : null;

    /// <summary>
    /// The source locations that the introspection file records: the position where each target declaration of an aspect instance
    /// starts.
    /// </summary>
    /// <remarks>
    /// The file records where a target declaration starts and the last line of its declaration syntax. A line is attributed to the
    /// recorded declarations whose span contains it, the innermost first. A declaration without an end line, written by an earlier
    /// version, matches only its first line. A line in a declaration that the file does not record, such as a method to which no
    /// aspect applies, matches the recorded declarations that contain it, such as its type, and never a sibling declaration. The
    /// file is read again when the locations are first needed, because the model of the query can come from the cache of the
    /// commands.
    /// </remarks>
    private sealed class SourceLocations : IIntrospectionSourceLocations
    {
        private readonly IntrospectionTarget _target;
        private Dictionary<string, IntrospectionSourceLocation>? _locations;

        public SourceLocations( IntrospectionTarget target )
        {
            this._target = target;
        }

        public IntrospectionSourceLocation? GetLocation( IntrospectionDeclaration declaration )
            => this.GetLocations().GetValueOrDefault( declaration.QualifiedId );

        public IReadOnlyList<string> FindDeclarations( string file, int line )
        {
            var fullPath = Path.GetFullPath( file );

            // The declarations whose span contains the line. The innermost one starts last.
            return this.GetLocations()
                .Where(
                    l => l.Value.Line <= line && line <= (l.Value.EndLine ?? l.Value.Line)
                                              && string.Equals( Path.GetFullPath( l.Value.File ), fullPath, StringComparison.OrdinalIgnoreCase ) )
                .OrderByDescending( l => l.Value.Line )
                .ThenByDescending( l => l.Value.Column )
                .Select( l => l.Key.Substring( l.Key.IndexOf( ']', StringComparison.Ordinal ) + 1 ) )
                .Distinct( StringComparer.Ordinal )
                .ToList();
        }

        private Dictionary<string, IntrospectionSourceLocation> GetLocations()
        {
            if ( this._locations == null )
            {
                using var stream = File.OpenRead( this._target.FilePath );
                var model = IntrospectionV2Loader.Load( stream, this._target.AssemblyName, this._target.Project.Directory );

                this._locations = model.Declarations.Where( d => d.Location != null )
                    .ToDictionary( d => d.QualifiedId, d => d.Location!, StringComparer.Ordinal );
            }

            return this._locations;
        }
    }
}
