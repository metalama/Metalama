// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;

namespace Metalama.Framework.Engine.Templating.Mapping
{
    /// <summary>
    /// Represents a map between two text code files: a source and a target. A set of <see cref="TextPoint"/>
    /// (including a character, line and column) of the target file is mapped to a set of <see cref="TextPoint"/>
    /// in the source file.
    /// </summary>
    internal sealed partial class TextMapFile
    {
        private const ulong _signature = 0xdfdfce7c841fe388;

        // Map by position from the beginning of the file.
        private readonly SkipListDictionary<int, TextPointMapping> _mapsByTargetCharacter;

        // Map by line and column.
        private readonly SkipListDictionary<LinePosition, TextPointMapping> _mapsByTargetLinePosition = new();

        /// <summary>
        /// Gets the path of the source file (typically the hand-written source code).
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Gets the path of the target file (typically the transformed code).
        /// </summary>
        public string TargetPath { get; }

        private TextMapFile( string sourcePath, string targetPath )
        {
            this.SourcePath = sourcePath;
            this.TargetPath = targetPath;
            this._mapsByTargetCharacter = new SkipListDictionary<int, TextPointMapping>();
        }

        private TextMapFile(
            string sourcePath,
            string targetPath,
            SkipListDictionary<int, TextPointMapping> mapsByTargetCharacter )
        {
            this.SourcePath = sourcePath;
            this.TargetPath = targetPath;
            this._mapsByTargetCharacter = mapsByTargetCharacter;

            foreach ( var mapping in mapsByTargetCharacter.Values )
            {
                _ = this._mapsByTargetLinePosition.Add( mapping.Target.LinePosition, mapping );
            }
        }

        /// <summary>
        /// Serializes the current <see cref="TextMapFile"/> into a <see cref="Stream"/>.
        /// </summary>
        public void Write( Stream stream ) => this.Write( new BinaryWriter( stream ) );

        /// <summary>
        /// Serializes the current <see cref="TextMapFile"/> into a <see cref="BinaryWriter"/>.
        /// </summary>
        private void Write( BinaryWriter writer )
        {
            writer.Write( _signature );
            writer.Write( this.SourcePath );
            writer.Write( (int) SourceHashAlgorithm.None );
            writer.Write( this.TargetPath );
            writer.Write( (int) SourceHashAlgorithm.None );
            writer.Write( this._mapsByTargetCharacter.Count );

            foreach ( var mapping in this._mapsByTargetCharacter.Values )
            {
                mapping.Write( writer );
            }
        }

        /// <summary>
        /// Creates a <see cref="TextMapFile"/> for a transformed <see cref="SyntaxTree"/> that has annotations
        /// of an <see cref="ILocationAnnotationMap"/> that maps the transformed locations to source locations.
        /// </summary>
        /// <param name="targetSyntaxTree">The target (transformed) <see cref="SyntaxTree"/>.</param>
        /// <param name="annotationMap">The <see cref="ILocationAnnotationMap"/> that can map annotations inside <paramref name="targetSyntaxTree"/>
        /// to locations in the source syntax tree.</param>
        /// <returns>A <see cref="TextMapFile"/>, or <c>null</c> if <paramref name="targetSyntaxTree"/> has no <see cref="SyntaxTree.FilePath"/> or does
        /// not contain any node mapped to a source tree.</returns>
        public static TextMapFile? Create( SyntaxTree targetSyntaxTree, ILocationAnnotationMap annotationMap )
        {
            if ( string.IsNullOrEmpty( targetSyntaxTree.FilePath ) )
            {
                // We cannot create a map if it has no name.
                throw new ArgumentOutOfRangeException( nameof(targetSyntaxTree), "The FilePath property must not be empty." );
            }

            Visitor visitor = new( annotationMap );
            var syntaxNode = targetSyntaxTree.GetRoot();
            visitor.Visit( syntaxNode );

            if ( visitor.SourcePath == null )
            {
                // No location found.
                return null;
            }

            return new TextMapFile( visitor.SourcePath, targetSyntaxTree.FilePath, visitor.TextPointMappings );
        }

        /// <summary>
        /// Reads a <see cref="TextMapFile"/> from the file system.
        /// </summary>
        /// <param name="path">The full path of the serialized <see cref="TextMapFile"/>.</param>
        /// <returns>The deserialized <see cref="TextMapFile"/>, or <c>null</c> if the file does not exist or is incorrect.</returns>
        public static TextMapFile? Read( string path )
        {
            if ( !File.Exists( path ) )
            {
                return null;
            }

            using var stream = File.OpenRead( path );

            if ( !TryRead( new BinaryReader( stream ), out var file ) )
            {
                return null;
            }

            return file;
        }

        private static bool TryRead( BinaryReader reader, out TextMapFile? file )
        {
            file = null;

            try
            {
                if ( reader.ReadUInt64() != _signature )
                {
                    return false;
                }

                var sourcePath = reader.ReadString();

                if ( (SourceHashAlgorithm) reader.ReadInt32() != SourceHashAlgorithm.None )
                {
                    return false;
                }

                var targetPath = reader.ReadString();

                if ( (SourceHashAlgorithm) reader.ReadInt32() != SourceHashAlgorithm.None )
                {
                    return false;
                }

                var count = reader.ReadInt32();

                file = new TextMapFile( sourcePath, targetPath );

                for ( var i = 0; i < count; i++ )
                {
                    var mapping = TextPointMapping.Read( reader );
                    file._mapsByTargetCharacter.Add( mapping.Target.Character, mapping );
                    file._mapsByTargetLinePosition.Add( mapping.Target.LinePosition, mapping );
                }
            }
            catch ( EndOfStreamException )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a <see cref="Location"/> in the source file given a <see cref="TextSpan"/> in the target file.
        /// </summary>
        /// <param name="targetSpan">A <see cref="TextSpan"/> in the target file.</param>
        /// <returns>A <see cref="Location"/> in the source file, or <c>null</c> if <paramref name="targetSpan"/> could not be mapped.</returns>
        public Location? GetSourceLocation( TextSpan targetSpan )
        {
            if ( !this._mapsByTargetCharacter.TryGetGreatestSmallerOrEqualValue( targetSpan.Start, out var startMapping ) ||
                 !this._mapsByTargetCharacter.TryGetGreatestSmallerOrEqualValue( targetSpan.End, out var endMapping ) )
            {
                return null;
            }

            if ( startMapping.Source.Character > endMapping.Source.Character || startMapping.Source.LinePosition > endMapping.Source.LinePosition )
            {
                // Inconsistent mapping: return null location instead of throwing.
                return null;
            }

            return Location.Create(
                this.SourcePath,
                TextSpan.FromBounds( startMapping.Source.Character, endMapping.Source.Character ),
                new LinePositionSpan( startMapping.Source.LinePosition, endMapping.Source.LinePosition ) );
        }
    }
}