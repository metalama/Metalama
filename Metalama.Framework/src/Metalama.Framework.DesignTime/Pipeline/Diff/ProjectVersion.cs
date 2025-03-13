// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
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

        public ImmutableDictionary<string, SyntaxTreeVersion> SyntaxTrees { get; }

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
            ImmutableDictionary<string, SyntaxTreeVersion> syntaxTrees,
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

            var syntaxTreesBuilder = ImmutableDictionary.CreateBuilder<string, SyntaxTreeVersion>( StringComparer.Ordinal );

            var generatedSyntaxTrees = new List<SyntaxTree>();

            foreach ( var syntaxTree in compilation.SyntaxTrees )
            {
                cancellationToken.ThrowIfCancellationRequested();

                if ( SourceGeneratorHelper.IsGeneratedFile( syntaxTree ) )
                {
                    generatedSyntaxTrees.Add( syntaxTree );

                    continue;
                }

                if ( syntaxTreesBuilder.TryGetValue( syntaxTree.FilePath, out var existingTreeVersion ) )
                {
                    logger ??= serviceProvider?.GetLoggerFactory().GetLogger( nameof(ProjectVersion) );

                    if ( logger?.Warning is { } warningLogger )
                    {
                        if ( existingTreeVersion.SyntaxTree.GetRoot( cancellationToken ).IsEquivalentTo( syntaxTree.GetRoot( cancellationToken ) ) )
                        {
                            warningLogger.Log( $"Two trees with the path '{syntaxTree.FilePath}' and the same code are included in the compilation; ignoring the second one." );
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

                syntaxTreesBuilder.Add( syntaxTree.FilePath, syntaxTreeVersion );
            }

            var syntaxTreeVersions = syntaxTreesBuilder.ToImmutable();

            var compilationToAnalyze = generatedSyntaxTrees.Count > 0 ? compilation.RemoveSyntaxTrees( generatedSyntaxTrees ) : compilation;

            return new ProjectVersion(
                strategy,
                projectKey,
                compilation,
                compilationToAnalyze,
                syntaxTreeVersions,
                referencedCompilations,
                referencesPortableExecutables );
        }

        public ProjectKey ProjectKey { get; }

        public bool TryGetSyntaxTreeVersion( string path, out SyntaxTreeVersion syntaxTreeVersion )
            => this.SyntaxTrees.AssertNotNull().TryGetValue( path, out syntaxTreeVersion );

        public override string ToString() => this.Compilation.AssemblyName ?? this.GetType().Name;
    }
}