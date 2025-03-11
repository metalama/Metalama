// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

internal sealed partial class ProjectVersionProvider
{
    private sealed partial class Implementation
    {
        private readonly ConditionalWeakTable<Compilation, ChangeList> _cache = new();
        private readonly Dictionary<ProjectKey, WeakReference<Compilation>> _lastCompilationPerProject = new();
        private readonly DiffStrategy _metalamaDiffStrategy;
        private readonly DiffStrategy _nonMetalamaDiffStrategy;
        private readonly IMetalamaProjectClassifier _metalamaProjectClassifier;
        private readonly IServiceProvider _serviceProvider;

        public Implementation( GlobalServiceProvider serviceProvider, bool isTest )
        {
            var observer = serviceProvider.GetService<IDifferObserver>();
            this._metalamaDiffStrategy = new DiffStrategy( isTest, true, true, observer );
            this._nonMetalamaDiffStrategy = new DiffStrategy( isTest, false, true, observer );
            this._metalamaProjectClassifier = serviceProvider.GetRequiredService<IMetalamaProjectClassifier>();
            this._serviceProvider = serviceProvider.Underlying;
        }

        /// <summary>
        /// Computes an incremental <see cref="CompilationChanges"/> between an old compilation and a new compilation
        /// based on the values from the cache only, or returns <c>null</c> if the value was not found in the cache.
        /// </summary>
        private bool TryGetIncrementalChangesFromCache(
            Compilation oldCompilation,
            Compilation newCompilation,
            [NotNullWhen( true )] out CompilationChanges? exactChanges,
            out CompilationChangesHandle closestChanges )
        {
            if ( !this._cache.TryGetValue( oldCompilation, out var list ) )
            {
                // If the old compilation is not in the cache, we cannot compute any incremental change.

                exactChanges = null;
                closestChanges = default;

                return false;
            }

            // If the new compilation is exactly the old compilation, there is no change.
            if ( newCompilation == oldCompilation )
            {
                exactChanges = CompilationChanges.Empty( list.ProjectVersion, list.ProjectVersion );
                closestChanges = default;

                return true;
            }

            // Find an available incremental change in the list.
            if ( list.TryGetIncrementalChanges( newCompilation, out exactChanges ) )
            {
                closestChanges = default;

                return true;
            }

            // We did not find the incremental changes for the pair of compilations, so we return the incremental changes
            // from the given original compilation to the last known compilation, which is hopefully very similar to the
            // new compilation because of the time correlation, as it should have just a few user edits.
            closestChanges = list.LastChanges?.ToHandle() ?? default;
            exactChanges = null;

            return false;
        }

        public async ValueTask<ProjectVersion> GetCompilationVersionCoreAsync(
            Compilation? oldCompilation,
            Compilation newCompilation,
            TestableCancellationToken cancellationToken = default )
        {
            // When we are asked a CompilationVersion, we do it through getting a CompilationChanges, because this path is incremental
            // and offers optimal performances.
            var changes = await this.GetCompilationChangesAsyncCoreAsync( oldCompilation, newCompilation, cancellationToken );

            return changes.NewProjectVersion;
        }

        private async ValueTask<ReferenceChanges> GetReferencesAsync(
            Compilation? oldCompilation,
            Compilation newCompilation,
            TestableCancellationToken cancellationToken )
        {
            // If references are the same by reference, there is no need to compare anything. Most hits to the method should take this shortcut.
            if ( oldCompilation != null && oldCompilation.ExternalReferences == newCompilation.ExternalReferences
                                        && this._cache.TryGetValue( oldCompilation, out var list ) )
            {
                return new ReferenceChanges(
                    list.ProjectVersion.ReferencedProjectVersions,
                    ImmutableDictionary<ProjectKey, ReferencedProjectChange>.Empty,
                    list.ProjectVersion.ReferencedPortableExecutables,
                    ImmutableDictionary<string, ReferenceChangeKind>.Empty );
            }

            var projectReferences = await this.GetProjectReferencesAsync( oldCompilation, newCompilation, cancellationToken );
            var portableExecutableReferences = GetPortableExecutableReferences( oldCompilation, newCompilation );

            return new ReferenceChanges(
                projectReferences.References,
                projectReferences.Changes,
                portableExecutableReferences.References,
                portableExecutableReferences.Changes );
        }

        private async Task<(ImmutableDictionary<ProjectKey, ReferencedProjectChange> Changes, ImmutableDictionary<ProjectKey, IProjectVersion> References)>
            GetProjectReferencesAsync(
                Compilation? oldCompilation,
                Compilation newCompilation,
                TestableCancellationToken cancellationToken )
        {
            // Verify changes in referenced projects.
            var changeListBuilder = ImmutableDictionary.CreateBuilder<ProjectKey, ReferencedProjectChange>();
            var referenceListBuilder = ImmutableDictionary.CreateBuilder<ProjectKey, IProjectVersion>();

            var oldProjectReferences = oldCompilation?.ExternalReferences.OfType<CompilationReference>()
                .ToDictionary( x => x.Compilation.GetProjectKey(), x => x.Compilation );

            var newProjectReferences = newCompilation.ExternalReferences.OfType<CompilationReference>().ToReadOnlyList();

            foreach ( var reference in newProjectReferences )
            {
                ReferencedProjectChange? changes;
                IProjectVersion projectVersion;

                cancellationToken.ThrowIfCancellationRequested();

                var assemblyIdentity = reference.Compilation.GetProjectKey();

                if ( oldCompilation != null && oldProjectReferences!.TryGetValue( assemblyIdentity, out var oldReferenceCompilation ) )
                {
                    var compilationChanges = await this.GetCompilationChangesAsyncCoreAsync(
                        oldReferenceCompilation,
                        reference.Compilation,
                        cancellationToken );

                    projectVersion = compilationChanges.NewProjectVersion;

                    if ( oldReferenceCompilation != reference.Compilation )
                    {
                        // We store the changes object even if there is no change, because of project versions.
                        changes = new ReferencedProjectChange(
                            oldReferenceCompilation,
                            reference.Compilation,
                            compilationChanges.HasChange ? ReferenceChangeKind.Modified : ReferenceChangeKind.None,
                            compilationChanges );
                    }
                    else
                    {
                        Invariant.Assert( !compilationChanges.HasChange );

                        // No change, not even compilation instances.
                        changes = null;
                    }
                }
                else
                {
                    // If there is no old compilation, the reference is new.
                    changes = new ReferencedProjectChange( null, reference.Compilation, ReferenceChangeKind.Added );
                    projectVersion = await this.GetCompilationVersionCoreAsync( null, reference.Compilation, cancellationToken );
                }

                if ( changes != null )
                {
                    changeListBuilder.Add( assemblyIdentity, changes.Value );
                }

                referenceListBuilder.Add( assemblyIdentity, projectVersion );
            }

            // Check removed references.
            if ( oldCompilation != null )
            {
                var referencedAssemblyIdentifies =
                    new HashSet<ProjectKey>( newProjectReferences.SelectAsImmutableArray( x => x.Compilation.GetProjectKey() ) );

                foreach ( var reference in oldProjectReferences! )
                {
                    if ( !referencedAssemblyIdentifies.Contains( reference.Key ) )
                    {
                        changeListBuilder.Add(
                            reference.Key,
                            new ReferencedProjectChange( reference.Value, null, ReferenceChangeKind.Removed ) );
                    }
                }
            }

            return (changeListBuilder.ToImmutable(), referenceListBuilder.ToImmutable());
        }

        private static (ImmutableDictionary<string, ReferenceChangeKind> Changes, ImmutableHashSet<string> References)
            GetPortableExecutableReferences(
                Compilation? oldCompilation,
                Compilation newCompilation )
        {
            var changeListBuilder = ImmutableDictionary.CreateBuilder<string, ReferenceChangeKind>( StringComparer.Ordinal );

            var oldReferences = oldCompilation?.ExternalReferences.OfType<PortableExecutableReference>()
                .Select( r => r.FilePath )
                .WhereNotNull()
                .ToImmutableHashSet( StringComparer.Ordinal );

            var newReferences = newCompilation.ExternalReferences.OfType<PortableExecutableReference>()
                .Select( x => x.FilePath )
                .WhereNotNull()
                .ToImmutableHashSet( StringComparer.Ordinal );

            foreach ( var reference in newReferences )
            {
                if ( oldReferences == null || !oldReferences.Contains( reference ) )
                {
                    changeListBuilder.Add( reference, ReferenceChangeKind.Added );
                }
            }

            if ( oldCompilation != null )
            {
                foreach ( var reference in oldReferences! )
                {
                    if ( !newReferences.Contains( reference ) )
                    {
                        changeListBuilder.Add( reference, ReferenceChangeKind.Removed );
                    }
                }
            }

            return (changeListBuilder.ToImmutable(), newReferences);
        }

        public async ValueTask<CompilationChanges> GetCompilationChangesAsyncCoreAsync(
            Compilation? oldCompilation,
            Compilation newCompilation,
            TestableCancellationToken cancellationToken = default )
        {
            DiffStrategy? diffStrategy = null;

            DiffStrategy GetDiffStrategy()
            {
                return diffStrategy ??= this._metalamaProjectClassifier.TryGetMetalamaVersion( newCompilation, out _ )
                    ? this._metalamaDiffStrategy
                    : this._nonMetalamaDiffStrategy;
            }

            if ( oldCompilation == null )
            {
                // If we were not given an old compilation, we will return a non-incremental changes (i.e., from an empty compilation).

                if ( this._cache.TryGetValue( newCompilation, out var newList ) )
                {
                    return newList.NonIncrementalChanges;
                }

                GetDiffStrategy().Observer?.OnNewCompilation();

                if ( !this._cache.TryGetValue( newCompilation, out newList ) )
                {
                    var referencedCompilationChanges = await this.GetReferencesAsync( oldCompilation, newCompilation, cancellationToken );

                    var compilationVersion = ProjectVersion.Create(
                        newCompilation,
                        newCompilation.GetProjectKey(),
                        GetDiffStrategy(),
                        referencedCompilationChanges.NewProjectReferences,
                        referencedCompilationChanges.NewPortableExecutableReferences,
                        this._serviceProvider,
                        cancellationToken );

                    newList = new ChangeList( compilationVersion );
                    this._cache.Add( newCompilation, newList );

                    this._lastCompilationPerProject[newCompilation.GetProjectKey()] =
                        new WeakReference<Compilation>( newCompilation );
                }

                return newList.NonIncrementalChanges;
            }
            else
            {
                // Find the pre-computed incremental changes from the graph. 

                if ( this.TryGetIncrementalChangesFromCache(
                        oldCompilation,
                        newCompilation,
                        out var incrementalChanges,
                        out var closestIncrementalChanges ) )
                {
                    // We already computed the changes between this exact pair of compilations.

                    return incrementalChanges;
                }
                else if ( closestIncrementalChanges.Value != null )
                {
                    // We do not have the exact pair of compilations in the cache, however we have already computed a diff from the same old
                    // compilation, so it's a good idea to compute the diff from the last known compilation instead of from the initial compilation,
                    // as it should contain fewer changes.

                    var references = await this.GetReferencesAsync(
                        closestIncrementalChanges.Value.NewProjectVersion.Compilation,
                        newCompilation,
                        cancellationToken );

                    var changesFromClosestCompilation = CompilationChanges.Incremental(
                        closestIncrementalChanges.Value.NewProjectVersion,
                        newCompilation,
                        references,
                        cancellationToken );

                    incrementalChanges = await this.MergeCompilationChangesAsync(
                        closestIncrementalChanges,
                        changesFromClosestCompilation,
                        cancellationToken );

                    // MergeCompilationChangesAsync requires that the oldCompilation is not collected by the GC.
                    // This is technically not necessary, since oldCompilation is also used afterwards, but it's here to make that requirement explicit.
                    GC.KeepAlive( oldCompilation );

                    if ( this._cache.TryGetValue( oldCompilation, out var changeLinkedListFromOldCompilation ) )
                    {
                        changeLinkedListFromOldCompilation.Add( incrementalChanges );
                    }

                    if ( !this._cache.TryGetValue( newCompilation, out _ ) )
                    {
                        this._cache.Add( newCompilation, new ChangeList( incrementalChanges.NewProjectVersion ) );
                    }
                    else
                    {
                        // This happens when the new compilation was already in the cache, but change to it from the old compilation was not.
                        // This means there will be multiple ProjectVersion objects for the same Compilation instance, but that shouldn't cause any issues here.
                    }

                    return incrementalChanges;
                }
                else
                {
                    // We could not get the changes from the cache, so we need to run the diff algorithm.

                    ProjectVersion oldProjectVersion;

                    if ( this._cache.TryGetValue( oldCompilation, out var changeLinkedListFromOldCompilation ) )
                    {
                        oldProjectVersion = changeLinkedListFromOldCompilation.ProjectVersion;
                    }
                    else
                    {
                        // We have never processed the old compilation, so we have to compute it from the scratch.

                        // Get the last compilation we know for the project. It can help building the old CompilationVersion incrementally.
                        Compilation? lastCompilationOfProject = null;

                        if ( this._lastCompilationPerProject.TryGetValue(
                                oldCompilation.GetProjectKey(),
                                out var lastCompilationOfProjectRef ) )
                        {
                            _ = lastCompilationOfProjectRef.TryGetTarget( out lastCompilationOfProject );
                        }

                        var oldReferences = await this.GetReferencesAsync( lastCompilationOfProject, oldCompilation, cancellationToken );

                        if ( lastCompilationOfProject != null )
                        {
                            // Build oldCompilationVersion incrementally.
                            oldProjectVersion = await this.GetCompilationVersionCoreAsync(
                                lastCompilationOfProject,
                                oldCompilation,
                                cancellationToken );

                            if ( !this._cache.TryGetValue( oldCompilation, out changeLinkedListFromOldCompilation ) )
                            {
                                throw new AssertionFailedException( $"Compilation '{oldCompilation.Assembly.Identity}' not found in cache." );
                            }
                        }
                        else
                        {
                            // Build oldCompilationVersion from scratch.
                            oldProjectVersion = ProjectVersion.Create(
                                oldCompilation,
                                oldCompilation.GetProjectKey(),
                                GetDiffStrategy(),
                                oldReferences.NewProjectReferences,
                                oldReferences.NewPortableExecutableReferences,
                                this._serviceProvider,
                                cancellationToken );

                            changeLinkedListFromOldCompilation = new ChangeList( oldProjectVersion );
                            this._cache.Add( oldCompilation, changeLinkedListFromOldCompilation );
                        }
                    }

                    var referenceChanges = await this.GetReferencesAsync(
                        oldCompilation,
                        newCompilation,
                        cancellationToken );

                    // Compute the increment.
                    incrementalChanges = CompilationChanges.Incremental(
                        oldProjectVersion,
                        newCompilation,
                        referenceChanges,
                        cancellationToken );

                    if ( !this._cache.TryGetValue( newCompilation, out _ ) )
                    {
                        this._cache.Add( newCompilation, new ChangeList( incrementalChanges.NewProjectVersion ) );

                        this._lastCompilationPerProject[newCompilation.GetProjectKey()] =
                            new WeakReference<Compilation>( newCompilation );
                    }
                    else
                    {
                        // The new compilation was already computed, but we could not reuse it because no diff from the old
                        // compilation was available.
                    }

                    changeLinkedListFromOldCompilation.Add( incrementalChanges );

                    return incrementalChanges;
                }
            }
        }

        private async ValueTask<CompilationChanges> MergeCompilationChangesAsync(
            CompilationChangesHandle firstHandle,
            CompilationChanges second,
            TestableCancellationToken cancellationToken )
        {
            var first = firstHandle.Value.AssertNotNull();

            if ( !second.IsIncremental )
            {
                return second;
            }
            else if ( !first.HasChange )
            {
                return new CompilationChanges(
                    firstHandle.OldProjectVersion,
                    second.NewProjectVersion,
                    second.SyntaxTreeChanges,
                    second.ReferencedCompilationChanges,
                    second.ReferencedPortableExecutableChanges,
                    second.AssemblyIdentityChanged,
                    second.HasCompileTimeCodeChange,
                    second.IsIncremental );
            }
            else if ( !second.HasChange )
            {
                return new CompilationChanges(
                    firstHandle.OldProjectVersion,
                    second.NewProjectVersion,
                    first.SyntaxTreeChanges,
                    first.ReferencedCompilationChanges,
                    first.ReferencedPortableExecutableChanges,
                    first.AssemblyIdentityChanged,
                    first.HasCompileTimeCodeChange,
                    first.IsIncremental );
            }
            else
            {
                this._metalamaDiffStrategy.Observer?.OnMergeCompilationChanges();

                // Merge syntax tree changes.
                var mergedSyntaxTreeBuilder = first.SyntaxTreeChanges.ToBuilder();

                foreach ( var syntaxTreeChanges in second.SyntaxTreeChanges )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( !mergedSyntaxTreeBuilder.TryGetValue( syntaxTreeChanges.Key, out var oldSyntaxTreeChange ) )
                    {
                        mergedSyntaxTreeBuilder.Add( syntaxTreeChanges );
                    }
                    else
                    {
                        var merged = oldSyntaxTreeChange.Merge( syntaxTreeChanges.Value );

                        if ( merged.SyntaxTreeChangeKind == SyntaxTreeChangeKind.None )
                        {
                            mergedSyntaxTreeBuilder.Remove( syntaxTreeChanges );
                        }
                        else
                        {
                            mergedSyntaxTreeBuilder[syntaxTreeChanges.Key] = merged;
                        }
                    }
                }

                // Merge changes in referenced compilations.
                var mergedReferencedCompilationBuilder = first.ReferencedCompilationChanges.ToBuilder();

                foreach ( var referencedCompilationChange in second.ReferencedCompilationChanges )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( !mergedReferencedCompilationBuilder.TryGetValue( referencedCompilationChange.Key, out var oldReferencedCompilationChange ) )
                    {
                        mergedReferencedCompilationBuilder.Add( referencedCompilationChange );
                    }
                    else
                    {
                        var merged = await this.MergeReferencedProjectChangesAsync(
                            oldReferencedCompilationChange,
                            referencedCompilationChange.Value,
                            cancellationToken );

                        if ( merged == null )
                        {
                            // First change added the reference, second change removed it, so it's as if it was never there.
                            mergedReferencedCompilationBuilder.Remove( referencedCompilationChange.Key );
                        }
                        else
                        {
                            mergedReferencedCompilationBuilder[referencedCompilationChange.Key] = merged.Value;
                        }
                    }
                }

                // Merge changes in referenced portable executable.
                var mergedReferencedPortableExecutablesBuilder = first.ReferencedPortableExecutableChanges.ToBuilder();

                foreach ( var referencedPortableExecutableChange in second.ReferencedPortableExecutableChanges )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( !mergedReferencedPortableExecutablesBuilder.TryGetValue( referencedPortableExecutableChange.Key, out var oldChange ) )
                    {
                        mergedReferencedPortableExecutablesBuilder.Add( referencedPortableExecutableChange );
                    }
                    else
                    {
                        var merged = MergePortableExecutableChanges(
                            oldChange,
                            referencedPortableExecutableChange.Value );

                        if ( merged == ReferenceChangeKind.None )
                        {
                            mergedReferencedPortableExecutablesBuilder.Remove( referencedPortableExecutableChange.Key );
                        }
                        else
                        {
                            mergedReferencedPortableExecutablesBuilder[referencedPortableExecutableChange.Key] = merged;
                        }
                    }
                }

                return new CompilationChanges(
                    firstHandle.OldProjectVersion,
                    second.NewProjectVersion,
                    mergedSyntaxTreeBuilder.ToImmutable(),
                    mergedReferencedCompilationBuilder.ToImmutable(),
                    mergedReferencedPortableExecutablesBuilder.ToImmutable(),
                    first.AssemblyIdentityChanged | second.AssemblyIdentityChanged,
                    first.HasCompileTimeCodeChange | second.HasCompileTimeCodeChange,
                    first.IsIncremental );
            }
        }

        private async ValueTask<ReferencedProjectChange?> MergeReferencedProjectChangesAsync(
            ReferencedProjectChange first,
            ReferencedProjectChange second,
            TestableCancellationToken cancellationToken = default )
        {
            // This operation is safe because it is called from the MergeCompilationChanges method
            // where we are sure that the old compilation and all references exist.
            var firstOldCompilation = first.OldCompilationDangerous;

            switch (first.ChangeKind, second.ChangeKind)
            {
                case (_, ReferenceChangeKind.None):
                    {
                        var changes = first.Changes == null
                            ? null
                            : new CompilationChanges(
                                first.Changes.OldProjectVersionDangerous,
                                second.Changes.AssertNotNull().NewProjectVersion,
                                first.Changes.SyntaxTreeChanges,
                                first.Changes.ReferencedCompilationChanges,
                                first.Changes.ReferencedPortableExecutableChanges,
                                first.Changes.AssemblyIdentityChanged,
                                first.Changes.HasCompileTimeCodeChange,
                                first.Changes.IsIncremental );

                        return new( firstOldCompilation, second.NewCompilation, first.ChangeKind, changes );
                    }

                case (ReferenceChangeKind.None, _):
                    {
                        var changes = second.Changes == null
                            ? null
                            : new CompilationChanges(
                                first.Changes.AssertNotNull().OldProjectVersionDangerous,
                                second.Changes.NewProjectVersion,
                                second.Changes.SyntaxTreeChanges,
                                second.Changes.ReferencedCompilationChanges,
                                second.Changes.ReferencedPortableExecutableChanges,
                                second.Changes.AssemblyIdentityChanged,
                                second.Changes.HasCompileTimeCodeChange,
                                second.Changes.IsIncremental );

                        return new( firstOldCompilation, second.NewCompilation, second.ChangeKind, changes );
                    }

                case (ReferenceChangeKind.Removed, ReferenceChangeKind.Added):
                    {
                        var changes = await this.GetCompilationChangesAsyncCoreAsync(
                            firstOldCompilation.AssertNotNull(),
                            second.NewCompilation.AssertNotNull(),
                            cancellationToken );

                        return new(
                            firstOldCompilation,
                            second.NewCompilation,
                            ReferenceChangeKind.Modified,
                            changes );
                    }

                case (ReferenceChangeKind.Added, ReferenceChangeKind.Removed):
                    return null;

                case (ReferenceChangeKind.Modified, ReferenceChangeKind.Modified):
                    {
                        var changes = await this.MergeCompilationChangesAsync( first.Changes!.ToHandle(), second.Changes!, cancellationToken );

                        return new(
                            firstOldCompilation,
                            second.NewCompilation,
                            changes.HasChange ? ReferenceChangeKind.Modified : ReferenceChangeKind.None,
                            changes );
                    }

                case (ReferenceChangeKind.Added, ReferenceChangeKind.Modified):
                    return new( null, second.NewCompilation, ReferenceChangeKind.Added );

                case (ReferenceChangeKind.Modified, ReferenceChangeKind.Removed):
                    return new( firstOldCompilation, null, ReferenceChangeKind.Removed );

                default:
                    throw new AssertionFailedException( $"Unexpected combination: ({first.ChangeKind}, {second.ChangeKind})" );
            }
        }

        private static ReferenceChangeKind MergePortableExecutableChanges(
            ReferenceChangeKind first,
            ReferenceChangeKind second )
        {
            switch (first, second)
            {
                case (_, ReferenceChangeKind.None):
                    return first;

                case (ReferenceChangeKind.None, _):
                    return second;

                case (ReferenceChangeKind.Removed, ReferenceChangeKind.Added):
                    return ReferenceChangeKind.None;

                case (ReferenceChangeKind.Added, ReferenceChangeKind.Removed):
                    return ReferenceChangeKind.None;

                case (ReferenceChangeKind.Added, ReferenceChangeKind.Modified):
                    return ReferenceChangeKind.Added;

                case (ReferenceChangeKind.Modified, ReferenceChangeKind.Removed):
                    return ReferenceChangeKind.Removed;

                default:
                    throw new AssertionFailedException( $"Unexpected combination: ({first}, {second})" );
            }
        }
    }
}