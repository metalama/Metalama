// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Diff
{
    /// <summary>
    /// The main implementation of <see cref="IProjectVersion"/>.
    /// </summary>
    internal sealed class ProjectVersion : IProjectVersion
    {
        public DiffStrategy Strategy { get; }

        public ImmutableDictionary<DocumentKey, SyntaxTreeVersion> SyntaxTrees { get; }

        public ImmutableDictionary<ProjectKey, IProjectVersion> ReferencedProjectVersions { get; }

        public ImmutableHashSet<string> ReferencedPortableExecutables { get; }

        public Compilation Compilation { get; }

        /// <summary>
        /// Gets the compilation that should be analyzed by the pipeline. This is typically an older version of
        /// the current <see cref="ProjectVersion"/>, but without the generated syntax trees.
        /// </summary>
        public Compilation CompilationToAnalyze { get; }

        public ProjectVersion(
            DiffStrategy strategy,
            ProjectKey projectKey,
            Compilation compilation,
            Compilation compilationToAnalyze,
            ImmutableDictionary<DocumentKey, SyntaxTreeVersion> syntaxTrees,
            ImmutableDictionary<ProjectKey, IProjectVersion> referencedCompilations,
            ImmutableHashSet<string> referencesPortableExecutables )
        {
            this.Strategy = strategy;
            this.SyntaxTrees = syntaxTrees;
            this.ReferencedProjectVersions = referencedCompilations;
            this.ReferencedPortableExecutables = referencesPortableExecutables;
            this.Compilation = compilation;
            this.ProjectKey = projectKey;
            this.CompilationToAnalyze = compilationToAnalyze;
        }

        public static ProjectVersion Create(
            Compilation compilation,
            ProjectKey projectKey,
            DiffStrategy strategy,
            ImmutableDictionary<ProjectKey, IProjectVersion>? referencedCompilations = null, // Can be null for test scenarios.
            ImmutableHashSet<string>? referencesPortableExecutables = null,
            IServiceProvider? serviceProvider = null,
            CancellationToken cancellationToken = default )
        {
            ILogger? logger = null;

            referencedCompilations ??= ImmutableDictionary<ProjectKey, IProjectVersion>.Empty;
            referencesPortableExecutables ??= ImmutableHashSet<string>.Empty;

            var syntaxTreesBuilder = ImmutableDictionary.CreateBuilder<DocumentKey, SyntaxTreeVersion>();

            var generatedSyntaxTrees = new List<SyntaxTree>();
            List<SyntaxTree>? duplicatePathSyntaxTrees = null;

            foreach ( var syntaxTree in compilation.SyntaxTrees )
            {
                cancellationToken.ThrowIfCancellationRequested();

                if ( SourceGeneratorHelper.IsGeneratedFile( syntaxTree ) )
                {
                    generatedSyntaxTrees.Add( syntaxTree );

                    continue;
                }

                if ( syntaxTreesBuilder.TryGetValue( syntaxTree.GetDocumentKey(), out var existingTreeVersion ) )
                {
                    // The tree is removed from the compilation and not merely skipped here. A version index that
                    // excludes a tree the analysed compilation still contains describes a compilation it does not
                    // match, and the pipeline would then leave that tree unrewritten while its declarations remain
                    // visible in the code model. See issue #1742.
                    duplicatePathSyntaxTrees ??= new List<SyntaxTree>();
                    duplicatePathSyntaxTrees.Add( syntaxTree );

                    logger ??= serviceProvider?.GetLoggerFactory().GetLogger( nameof(ProjectVersion) );

                    if ( logger?.Warning is { } warningLogger )
                    {
                        if ( existingTreeVersion.SyntaxTree.GetRoot( cancellationToken ).IsEquivalentTo( syntaxTree.GetRoot( cancellationToken ) ) )
                        {
                            warningLogger.Log(
                                $"Two trees with the path '{syntaxTree.FilePath}' and the same code are included in the compilation; ignoring the second one." );
                        }
                        else
                        {
                            warningLogger.Log(
                                $"""
                                 Two trees with the path '{syntaxTree.FilePath}' and different code are included in the compilation; ignoring the second one.
                                 Tree 1:
                                 {existingTreeVersion.SyntaxTree}
                                 Tree 2:
                                 {syntaxTree}
                                 """ );
                        }
                    }

                    continue;
                }

                var syntaxTreeVersion = strategy.GetSyntaxTreeVersion( syntaxTree, compilation );

                syntaxTreesBuilder.Add( syntaxTree.GetDocumentKey(), syntaxTreeVersion );
            }

            var syntaxTreeVersions = syntaxTreesBuilder.ToImmutable();

            var treesToRemove = Concat( generatedSyntaxTrees, duplicatePathSyntaxTrees );
            var compilationToAnalyze = treesToRemove.Count > 0 ? compilation.RemoveSyntaxTrees( treesToRemove ) : compilation;

            return new ProjectVersion(
                strategy,
                projectKey,
                compilation,
                compilationToAnalyze,
                syntaxTreeVersions,
                referencedCompilations,
                referencesPortableExecutables );
        }

        /// <summary>
        /// Concatenates the two lists of syntax trees to remove from the compilation, avoiding an allocation in the
        /// ordinary case where at most one of them has content.
        /// </summary>
        private static IReadOnlyList<SyntaxTree> Concat( List<SyntaxTree> generated, List<SyntaxTree>? duplicatePaths )
        {
            if ( duplicatePaths == null )
            {
                return generated;
            }

            if ( generated.Count == 0 )
            {
                return duplicatePaths;
            }

            var all = new List<SyntaxTree>( generated.Count + duplicatePaths.Count );
            all.AddRange( generated );
            all.AddRange( duplicatePaths );

            return all;
        }

        public ProjectKey ProjectKey { get; }

        public bool TryGetSyntaxTreeVersion( DocumentKey documentKey, out SyntaxTreeVersion syntaxTreeVersion )
            => this.SyntaxTrees.AssertNotNull().TryGetValue( documentKey, out syntaxTreeVersion );

        public override string ToString() => this.Compilation.AssemblyName ?? nameof(ProjectVersion);
    }
}