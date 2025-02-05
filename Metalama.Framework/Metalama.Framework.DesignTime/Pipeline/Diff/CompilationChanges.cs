// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Diff
{
    /// <summary>
    /// Represents changes between two instances of the <see cref="Microsoft.CodeAnalysis.Compilation"/> class.
    /// </summary>
    internal sealed class CompilationChanges
    {
        private readonly WeakReference<ProjectVersion>? _oldProjectVersionRef;

        public ImmutableDictionary<string, SyntaxTreeChange> SyntaxTreeChanges { get; }

        public ProjectVersion? OldProjectVersionDangerous
        {
            get
            {
                if ( this._oldProjectVersionRef == null )
                {
                    return null;
                }

                if ( this._oldProjectVersionRef.TryGetTarget( out var version ) )
                {
                    return version;
                }

                throw new InvalidOperationException( "The old compilation is no longer alive." );
            }
        }

        public CompilationChangesHandle ToHandle()
        {
            if ( this._oldProjectVersionRef == null )
            {
                return new CompilationChangesHandle( this, null );
            }
            else if ( this._oldProjectVersionRef.TryGetTarget( out var version ) )
            {
                return new CompilationChangesHandle( this, version );
            }
            else
            {
                return default;
            }
        }

        public ProjectVersion NewProjectVersion { get; }

        public ImmutableDictionary<ProjectKey, ReferencedProjectChange> ReferencedCompilationChanges { get; }

        public ImmutableDictionary<string, ReferenceChangeKind> ReferencedPortableExecutableChanges { get; }

        public bool AssemblyIdentityChanged { get; }

        public ProjectKey ProjectKey => this.NewProjectVersion.ProjectKey;

        /// <summary>
        /// Gets a value indicating whether the changes affects the compile-time subproject.
        /// </summary>
        public bool HasCompileTimeCodeChange { get; }

        public CompilationChanges(
            ProjectVersion? oldProjectVersion,
            ProjectVersion newProjectVersion,
            ImmutableDictionary<string, SyntaxTreeChange> syntaxTreeChanges,
            ImmutableDictionary<ProjectKey, ReferencedProjectChange> referencedCompilationChanges,
            ImmutableDictionary<string, ReferenceChangeKind> referencedPortableExecutableChanges,
            bool assemblyIdentityChanged,
            bool hasCompileTimeCodeChange,
            bool isIncremental )
        {
            if ( isIncremental != (oldProjectVersion != null) )
            {
                throw new AssertionFailedException( "IsIncremental is not consistent." );
            }

            this.SyntaxTreeChanges = syntaxTreeChanges;
            this._oldProjectVersionRef = oldProjectVersion == null ? null : new WeakReference<ProjectVersion>( oldProjectVersion );
            this.NewProjectVersion = newProjectVersion;
            this.ReferencedCompilationChanges = referencedCompilationChanges;
            this.ReferencedPortableExecutableChanges = referencedPortableExecutableChanges;
            this.HasCompileTimeCodeChange = hasCompileTimeCodeChange;
            this.AssemblyIdentityChanged = assemblyIdentityChanged;
            this.IsIncremental = isIncremental;
        }

        /// <summary>
        /// Gets a <see cref="CompilationChanges"/> object that represents the absence of change.
        /// </summary>
        public static CompilationChanges Empty( ProjectVersion? oldProject, ProjectVersion newProject )
            => new(
                oldProject,
                newProject,
                ImmutableDictionary<string, SyntaxTreeChange>.Empty,
                ImmutableDictionary<ProjectKey, ReferencedProjectChange>.Empty,
                ImmutableDictionary<string, ReferenceChangeKind>.Empty,
                assemblyIdentityChanged: false,
                hasCompileTimeCodeChange: false,
                isIncremental: oldProject != null );

        public bool HasChange
            => this.HasCompileTimeCodeChange
               || this.SyntaxTreeChanges.Count > 0
               || this.ReferencedCompilationChanges.Count > 0
               || this.ReferencedPortableExecutableChanges.Count > 0;

        public bool IsIncremental { get; }

        public static CompilationChanges NonIncremental( ProjectVersion projectVersion )
        {
            projectVersion.Strategy.Observer?.OnComputeNonIncrementalChanges();

            var syntaxTreeChanges = projectVersion.SyntaxTrees.ToImmutableDictionary( t => t.Key, t => SyntaxTreeChange.NonIncremental( t.Value ) );

            var projectReferences = projectVersion.ReferencedProjectVersions.ToImmutableDictionary(
                x => x.Key,
                x => new ReferencedProjectChange( null, x.Value.Compilation, ReferenceChangeKind.Added ) );

            var portableExecutableReferences = projectVersion.ReferencedPortableExecutables
                .ToImmutableDictionary( x => x, _ => ReferenceChangeKind.Added );

            return new CompilationChanges(
                null,
                projectVersion,
                syntaxTreeChanges,
                projectReferences,
                portableExecutableReferences,
                assemblyIdentityChanged: true,
                hasCompileTimeCodeChange: true,
                isIncremental: false );
        }

        public static CompilationChanges Incremental(
            ProjectVersion oldProjectVersion,
            Compilation newCompilation,
            ReferenceChanges referenceChanges,
            TestableCancellationToken cancellationToken = default )
        {
            if ( newCompilation == oldProjectVersion.Compilation )
            {
                return Empty( oldProjectVersion, oldProjectVersion );
            }

            oldProjectVersion.Strategy.Observer?.OnComputeIncrementalChanges();

            var newTrees = ImmutableDictionary.CreateBuilder<string, SyntaxTreeVersion>( StringComparer.Ordinal );
            var generatedTrees = new List<SyntaxTree>();

            var syntaxTreeChanges = ImmutableDictionary.CreateBuilder<string, SyntaxTreeChange>( StringComparer.Ordinal );

            var hasCompileTimeChange = !referenceChanges.PortableExecutableReferenceChanges.IsEmpty
                                       || referenceChanges.ProjectReferenceChanges.Any( c => c.Value.HasCompileTimeCodeChange );

            // Change in the assembly identity (and assembly version in particular) should be considered a compile-time change.
            // This is important especially because on start, VS first creates compilations without assembly versions (0.0.0.0)
            // and only afterwards creates compilations with correct versions.
            var assemblyIdentityChanged = oldProjectVersion.Compilation.Assembly.Identity != newCompilation.Assembly.Identity;
            hasCompileTimeChange |= assemblyIdentityChanged;

            // Process new trees.
            var lastTrees = oldProjectVersion.SyntaxTrees;

            foreach ( var newSyntaxTree in newCompilation.SyntaxTrees )
            {
                cancellationToken.ThrowIfCancellationRequested();

                CompileTimeChangeKind compileTimeChangeKind;

                // Files generated by us are ignored during the comparison.
                if ( SourceGeneratorHelper.IsGeneratedFile( newSyntaxTree ) )
                {
                    generatedTrees.Add( newSyntaxTree );

                    continue;
                }

                // At design time, the collection of syntax trees can contain duplicates,
                // which may not even have the same text (e.g. one could contain `internal class C`, while the other just `class C`).
                if ( newTrees.TryGetValue( newSyntaxTree.FilePath, out var _ ) )
                {
                    continue;
                }

                SyntaxTreeVersion newSyntaxTreeVersion;

                if ( lastTrees.TryGetValue( newSyntaxTree.FilePath, out var oldSyntaxTreeVersion ) )
                {
                    if ( oldProjectVersion.Strategy.IsDifferent(
                            oldSyntaxTreeVersion,
                            newSyntaxTree,
                            newCompilation,
                            out newSyntaxTreeVersion ) )
                    {
                        compileTimeChangeKind = DiffStrategy.GetCompileTimeChangeKind(
                            oldSyntaxTreeVersion.HasCompileTimeCode,
                            newSyntaxTreeVersion.HasCompileTimeCode );

                        var change = new SyntaxTreeChange(
                            newSyntaxTree.FilePath,
                            SyntaxTreeChangeKind.Changed,
                            compileTimeChangeKind,
                            oldSyntaxTreeVersion,
                            newSyntaxTreeVersion );

                        syntaxTreeChanges.Add( newSyntaxTree.FilePath, change );

                        hasCompileTimeChange |= newSyntaxTreeVersion.HasCompileTimeCode || oldSyntaxTreeVersion.HasCompileTimeCode;
                    }
                }
                else
                {
                    // This is a new tree.
                    newSyntaxTreeVersion = oldProjectVersion.Strategy.GetSyntaxTreeVersion( newSyntaxTree, newCompilation );

                    compileTimeChangeKind = DiffStrategy.GetCompileTimeChangeKind( false, newSyntaxTreeVersion.HasCompileTimeCode );

                    var change = new SyntaxTreeChange(
                        newSyntaxTree.FilePath,
                        SyntaxTreeChangeKind.Added,
                        compileTimeChangeKind,
                        default,
                        newSyntaxTreeVersion );

                    syntaxTreeChanges.Add( newSyntaxTree.FilePath, change );

                    hasCompileTimeChange |= newSyntaxTreeVersion.HasCompileTimeCode;
                }

                newTrees.Add( newSyntaxTree.FilePath, newSyntaxTreeVersion );
                lastTrees = lastTrees.Remove( newSyntaxTree.FilePath );
            }

            // Process old trees.
            foreach ( var oldSyntaxTree in lastTrees )
            {
                syntaxTreeChanges.Add(
                    oldSyntaxTree.Key,
                    new SyntaxTreeChange(
                        oldSyntaxTree.Key,
                        SyntaxTreeChangeKind.Removed,
                        DiffStrategy.GetCompileTimeChangeKind( oldSyntaxTree.Value.HasCompileTimeCode, false ),
                        oldSyntaxTree.Value,
                        default ) );
            }

            // Create the new CompilationVersion.
            var syntaxTreeVersions = newTrees.ToImmutable();

            // We have to analyze a new compilation, however we need to remove generated trees.
            var compilationToAnalyze = newCompilation.RemoveSyntaxTrees( generatedTrees );

            var newCompilationVersion = new ProjectVersion(
                oldProjectVersion.Strategy,
                oldProjectVersion.ProjectKey,
                newCompilation,
                compilationToAnalyze,
                syntaxTreeVersions,
                referenceChanges.NewProjectReferences,
                referenceChanges.NewPortableExecutableReferences );

            cancellationToken.ThrowIfCancellationRequested();

            var compilationChanges = new CompilationChanges(
                oldProjectVersion,
                newCompilationVersion,
                syntaxTreeChanges.ToImmutable(),
                referenceChanges.ProjectReferenceChanges,
                referenceChanges.PortableExecutableReferenceChanges,
                assemblyIdentityChanged,
                hasCompileTimeChange,
                isIncremental: true );

            return compilationChanges;
        }

        public override string ToString() => $"HasCompileTimeCodeChange={this.HasCompileTimeCodeChange}, SyntaxTreeChanges={this.SyntaxTreeChanges.Count}";
    }
}