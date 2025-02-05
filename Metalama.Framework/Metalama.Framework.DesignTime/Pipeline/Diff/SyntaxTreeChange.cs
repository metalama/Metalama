// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Diff
{
    /// <summary>
    /// Represents a change between two versions of a <see cref="SyntaxTree"/>.
    /// </summary>
    internal readonly struct SyntaxTreeChange : IEquatable<SyntaxTreeChange>
    {
        private readonly WeakReference<SyntaxTree>? _oldSyntaxTreeRef;
        private readonly SyntaxTreeVersionData _oldSyntaxTreeVersionData;

        /// <summary>
        /// Gets the kind of change between the old and new syntax trees.
        /// </summary>
        public SyntaxTreeChangeKind SyntaxTreeChangeKind { get; }

        /// <summary>
        /// Gets a value indicating how the <see cref="HasCompileTimeCode"/> value has changed between the old
        /// and the new syntax tree.
        /// </summary>
        public CompileTimeChangeKind CompileTimeChangeKind { get; }

        /// <summary>
        /// Gets the path of the syntax tree.
        /// </summary>
        public string FilePath { get; }

        // ReSharper disable once MemberCanBePrivate.Global

        public SyntaxTreeVersion OldSyntaxTreeVersionDangerous
        {
            get
            {
                if ( this._oldSyntaxTreeRef == null )
                {
                    return default;
                }
                else if ( this._oldSyntaxTreeRef.TryGetTarget( out var syntaxTree ) )
                {
                    return new SyntaxTreeVersion( syntaxTree, this._oldSyntaxTreeVersionData );
                }
                else
                {
                    throw new InvalidOperationException( "The old syntax tree is no longer alive." );
                }
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly SyntaxTreeVersion NewSyntaxTreeVersion;

        /// <summary>
        /// Gets the list of partial types that have been added between the old version and the new version.
        /// </summary>
        public ImmutableArray<PartialTypeChange> PartialTypeChanges { get; }

        /// <summary>
        /// Gets the new syntax tree, unless the current item represents a deleted tree.
        /// </summary>
        public SyntaxTree NewTree => this.NewSyntaxTreeVersion.SyntaxTree;

        public ulong OldHash => this._oldSyntaxTreeVersionData.DeclarationHash;

        public ulong NewHash => this.NewSyntaxTreeVersion.DeclarationHash;

        /// <summary>
        /// Gets a value indicating whether the new syntax tree contains compile-time code.
        /// </summary>
        public bool HasCompileTimeCode => this.NewSyntaxTreeVersion.HasCompileTimeCode;

        /// <summary>
        /// Gets a value indicating whether the old syntax tree or the new syntax tree contain global attributes.
        /// </summary>
        public bool EitherHasGlobalAttributes => this._oldSyntaxTreeVersionData.HasGlobalAttributes || this.NewSyntaxTreeVersion.HasGlobalAttributes;

        public static SyntaxTreeChange NonIncremental( in SyntaxTreeVersion syntaxTreeVersion )
            => new(
                syntaxTreeVersion.SyntaxTree.FilePath,
                SyntaxTreeChangeKind.Added,
                syntaxTreeVersion.HasCompileTimeCode ? CompileTimeChangeKind.NewlyCompileTime : CompileTimeChangeKind.None,
                default,
                syntaxTreeVersion );

        public SyntaxTreeChange(
            string filePath,
            SyntaxTreeChangeKind syntaxTreeChangeKind,
            CompileTimeChangeKind compileTimeChangeKind,
            in SyntaxTreeVersion oldSyntaxTreeVersion,
            in SyntaxTreeVersion newSyntaxTreeVersion )
        {
            this.SyntaxTreeChangeKind = syntaxTreeChangeKind;
            this.CompileTimeChangeKind = compileTimeChangeKind;
            this.FilePath = filePath;
            this.NewSyntaxTreeVersion = newSyntaxTreeVersion;
            this._oldSyntaxTreeVersionData = new SyntaxTreeVersionData( oldSyntaxTreeVersion );
            this._oldSyntaxTreeRef = oldSyntaxTreeVersion.IsDefault ? null : new WeakReference<SyntaxTree>( oldSyntaxTreeVersion.SyntaxTree );

            // Detecting changes in partial types.
            this.PartialTypeChanges = ImmutableArray<PartialTypeChange>.Empty;

            switch ( syntaxTreeChangeKind )
            {
                case SyntaxTreeChangeKind.Changed when newSyntaxTreeVersion.PartialTypesHash != oldSyntaxTreeVersion.PartialTypesHash:
                    foreach ( var partialType in newSyntaxTreeVersion.PartialTypes )
                    {
                        if ( !oldSyntaxTreeVersion.PartialTypes.Contains( partialType ) )
                        {
                            this.PartialTypeChanges = this.PartialTypeChanges.Add( new PartialTypeChange( partialType, PartialTypeChangeKind.Added ) );
                        }
                    }

                    foreach ( var partialType in oldSyntaxTreeVersion.PartialTypes )
                    {
                        if ( !newSyntaxTreeVersion.PartialTypes.Contains( partialType ) )
                        {
                            this.PartialTypeChanges = this.PartialTypeChanges.Add( new PartialTypeChange( partialType, PartialTypeChangeKind.Removed ) );
                        }
                    }

                    break;

                case SyntaxTreeChangeKind.Added:
                    this.PartialTypeChanges = newSyntaxTreeVersion.PartialTypes.Select( t => new PartialTypeChange( t, PartialTypeChangeKind.Added ) )
                        .ToImmutableArray();

                    break;
            }
        }

        public override string ToString() => $"{this.FilePath}, ChangeKind={this.SyntaxTreeChangeKind}, CompileTimeChangeKind={this.CompileTimeChangeKind}";

        public SyntaxTreeChange Merge( in SyntaxTreeChange newChange )
        {
            var newSyntaxTreeChangeKind = (this.SyntaxTreeChangeKind, newChange.SyntaxTreeChangeKind) switch
            {
                (SyntaxTreeChangeKind.Added, SyntaxTreeChangeKind.Changed) => SyntaxTreeChangeKind.Added,
                (SyntaxTreeChangeKind.Added, SyntaxTreeChangeKind.Removed) => SyntaxTreeChangeKind.None,
                (SyntaxTreeChangeKind.Added, SyntaxTreeChangeKind.Added) => SyntaxTreeChangeKind.Added,
                (_, SyntaxTreeChangeKind.Removed) => SyntaxTreeChangeKind.Removed,
                (SyntaxTreeChangeKind.Removed, SyntaxTreeChangeKind.Added) when newChange.NewHash != this.OldHash => SyntaxTreeChangeKind.Changed,
                (SyntaxTreeChangeKind.Removed, SyntaxTreeChangeKind.Added) when newChange.NewHash == this.OldHash => SyntaxTreeChangeKind.None,
                (SyntaxTreeChangeKind.Removed, SyntaxTreeChangeKind.Changed) => SyntaxTreeChangeKind.Changed,
                (SyntaxTreeChangeKind.Changed, SyntaxTreeChangeKind.Changed) => SyntaxTreeChangeKind.Changed,
                _ => throw new AssertionFailedException(
                    $"Invalid SyntaxTreeChangeKind combination: ({this.SyntaxTreeChangeKind}, {newChange.SyntaxTreeChangeKind})." )
            };

            var newCompileTimeChangeKind = (this.CompileTimeChangeKind, newChange.CompileTimeChangeKind) switch
            {
                (_, CompileTimeChangeKind.None) => this.CompileTimeChangeKind,
                (CompileTimeChangeKind.None, _) => newChange.CompileTimeChangeKind,
                (CompileTimeChangeKind.NewlyCompileTime, CompileTimeChangeKind.NoLongerCompileTime) => CompileTimeChangeKind.None,
                (CompileTimeChangeKind.NewlyCompileTime, CompileTimeChangeKind.NewlyCompileTime) => CompileTimeChangeKind.NewlyCompileTime,
                (CompileTimeChangeKind.NoLongerCompileTime, CompileTimeChangeKind.NewlyCompileTime) => CompileTimeChangeKind.None,
                (CompileTimeChangeKind.NoLongerCompileTime, CompileTimeChangeKind.NoLongerCompileTime) => CompileTimeChangeKind.NoLongerCompileTime,

                _ => throw new AssertionFailedException(
                    $"Invalid CompileTimeChangeKind combination: ({this.CompileTimeChangeKind}, {newChange.CompileTimeChangeKind})." )
            };

            // This is called when we are sure in the merge of the old compilation to the new compilation,
            // so this operation is safe.
            var oldSyntaxTreeVersion = this.OldSyntaxTreeVersionDangerous;

            return new SyntaxTreeChange(
                this.FilePath,
                newSyntaxTreeChangeKind,
                newCompileTimeChangeKind,
                oldSyntaxTreeVersion,
                newChange.NewSyntaxTreeVersion );
        }

        public bool Equals( SyntaxTreeChange other )
            => ReferenceEquals( this._oldSyntaxTreeRef, other._oldSyntaxTreeRef )
               && this._oldSyntaxTreeVersionData.Equals( other._oldSyntaxTreeVersionData )
               && this.NewSyntaxTreeVersion.Equals( other.NewSyntaxTreeVersion )
               && this.SyntaxTreeChangeKind == other.SyntaxTreeChangeKind
               && this.CompileTimeChangeKind == other.CompileTimeChangeKind
               && this.FilePath == other.FilePath
               && this.PartialTypeChanges.Equals( other.PartialTypeChanges );

        public override bool Equals( object? obj ) => obj is SyntaxTreeChange other && this.Equals( other );

        public override int GetHashCode()
            => HashCode.Combine(
                this._oldSyntaxTreeRef,
                this._oldSyntaxTreeVersionData,
                this.NewSyntaxTreeVersion,
                (int) this.SyntaxTreeChangeKind,
                (int) this.CompileTimeChangeKind,
                this.FilePath,
                this.PartialTypeChanges );
    }
}