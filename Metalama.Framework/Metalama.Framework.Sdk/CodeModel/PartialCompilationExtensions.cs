// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.CodeModel
{
    /// <summary>
    /// Extension methods for the <see cref="IPartialCompilation"/> interface.
    /// </summary>
    [PublicAPI]
    public static class PartialCompilationExtensions
    {
        /// <summary>
        /// Updates the syntax trees of a given <see cref="IPartialCompilation"/> by providing a function that maps
        /// a <see cref="SyntaxTree"/> to a transformed <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="updateTree">A function that maps the old <see cref="SyntaxTree"/> to the new <see cref="SyntaxTree"/>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A new <see cref="IPartialCompilation"/>.</returns>
        public static IPartialCompilation UpdateSyntaxTrees(
            this IPartialCompilation compilation,
            Func<SyntaxTree, CancellationToken, SyntaxTree> updateTree,
            CancellationToken cancellationToken = default )
            => compilation.WithSyntaxTreeTransformations(
                compilation.SyntaxTrees.Values.Select( t => SyntaxTreeTransformation.ReplaceTree( t, updateTree( t, cancellationToken ) ) )
                    .Where( t => t.NewTree != t.OldTree )
                    .ToList() );

        /// <summary>
        /// Updates the syntax trees of a given <see cref="IPartialCompilation"/> by providing a function that maps
        /// a <see cref="SyntaxTree"/> to a transformed <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="updateSyntaxRoot">A function that maps the old root <see cref="SyntaxNode"/> to the new <see cref="SyntaxNode"/>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A new <see cref="IPartialCompilation"/>.</returns>
        public static IPartialCompilation UpdateSyntaxTrees(
            this IPartialCompilation compilation,
            Func<SyntaxNode, CancellationToken, SyntaxNode> updateSyntaxRoot,
            CancellationToken cancellationToken = default )
            => compilation.WithSyntaxTreeTransformations(
                compilation.SyntaxTrees.Values.Select( t => (OldTree: t, NewRoot: updateSyntaxRoot( t.GetRoot( cancellationToken ), cancellationToken )) )
                    .Where( x => x.OldTree.GetRoot( cancellationToken ) != x.NewRoot )
                    .Select(
                        x => SyntaxTreeTransformation.ReplaceTree(
                            x.OldTree,
                            x.OldTree.WithRootAndOptions( x.NewRoot, (CSharpParseOptions) x.OldTree.Options ) ) )
                    .ToList() );

        public static IPartialCompilation UpdateSyntaxTrees(
            this IPartialCompilation compilation,
            Func<SyntaxTree, SyntaxTree> updateTree,
            CancellationToken cancellationToken = default )
        {
            var modifiedSyntaxTrees = new List<SyntaxTreeTransformation>( compilation.SyntaxTrees.Count );

            foreach ( var tree in compilation.SyntaxTrees.Values )
            {
                var newTree = updateTree( tree );

                cancellationToken.ThrowIfCancellationRequested();

                if ( newTree != tree )
                {
                    modifiedSyntaxTrees.Add( SyntaxTreeTransformation.ReplaceTree( tree, newTree ) );
                }
            }

            return compilation.WithSyntaxTreeTransformations( modifiedSyntaxTrees );
        }

        public static Task<IPartialCompilation> RewriteSyntaxTreesAsync(
            this IPartialCompilation compilation,
            CSharpSyntaxRewriter rewriter,
            ProjectServiceProvider serviceProvider,
            CancellationToken cancellationToken = default )
            => compilation.RewriteSyntaxTreesAsync( _ => rewriter, serviceProvider, cancellationToken );

        public static async Task<IPartialCompilation> RewriteSyntaxTreesAsync(
            this IPartialCompilation compilation,
            Func<SyntaxNode, CSharpSyntaxRewriter> rewriterFactory,
            ProjectServiceProvider serviceProvider,
            CancellationToken cancellationToken = default )
        {
            var taskScheduler = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            var modifiedSyntaxTrees = new ConcurrentQueue<SyntaxTreeTransformation>();

            await taskScheduler.RunConcurrentlyAsync( compilation.SyntaxTrees, RewriteSyntaxTreeAsync, cancellationToken );

            async Task RewriteSyntaxTreeAsync( KeyValuePair<string, SyntaxTree> tree )
            {
                cancellationToken.ThrowIfCancellationRequested();

                var oldRoot = await tree.Value.GetRootAsync( cancellationToken );
                var newRoot = rewriterFactory( oldRoot ).Visit( oldRoot );

                if ( newRoot != oldRoot )
                {
                    modifiedSyntaxTrees.Enqueue( SyntaxTreeTransformation.ReplaceTree( tree.Value, tree.Value.WithRootAndOptions( newRoot, tree.Value.Options ) ) );
                }
            }

            return compilation.WithSyntaxTreeTransformations( modifiedSyntaxTrees.ToList() );
        }

        public static IPartialCompilation AddSyntaxTrees( this IPartialCompilation compilation, params SyntaxTree[] syntaxTrees )
            => compilation.WithSyntaxTreeTransformations( syntaxTrees.Select( SyntaxTreeTransformation.AddTree ).ToList() );

        public static IPartialCompilation AddSyntaxTrees( this IPartialCompilation compilation, IEnumerable<SyntaxTree> syntaxTrees )
            => compilation.WithSyntaxTreeTransformations( syntaxTrees.Select( SyntaxTreeTransformation.AddTree ).ToList() );

        /// <summary>
        /// Gets <see cref="ParseOptions"/> that should be used when adding new syntax trees to this compilation.
        /// </summary>
        public static ParseOptions GetParseOptions( this IPartialCompilation compilation )
        {
            var firstTree = compilation.SyntaxTrees.Values.FirstOrDefault();

            if ( firstTree == null )
            {
                return CSharpParseOptions.Default;
            }

            return firstTree.Options;
        }
    }
}