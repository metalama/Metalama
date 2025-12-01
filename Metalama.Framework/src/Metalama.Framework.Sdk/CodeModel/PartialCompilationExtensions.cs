// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// Provides extension methods for transforming <see cref="IPartialCompilation"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These methods simplify common compilation transformation patterns in <see cref="Metalama.Framework.Engine.AspectWeavers.IAspectWeaver"/> implementations:
    /// </para>
    /// <list type="bullet">
    /// <item><description>UpdateSyntaxTrees: Transform syntax trees using a delegate.</description></item>
    /// <item><description>RewriteSyntaxTreesAsync: Rewrite using a <see cref="CSharpSyntaxRewriter"/>.</description></item>
    /// <item><description><see cref="AddSyntaxTrees(IPartialCompilation, SyntaxTree[])"/>: Add new syntax trees.</description></item>
    /// <item><description><see cref="GetParseOptions"/>: Get parse options for creating new syntax trees.</description></item>
    /// </list>
    /// <para>
    /// All methods process syntax trees in parallel for performance. When using <see cref="RewriteSyntaxTreesAsync(IPartialCompilation, CSharpSyntaxRewriter, ProjectServiceProvider, CancellationToken)"/>
    /// with a shared rewriter, ensure the rewriter is thread-safe.
    /// </para>
    /// </remarks>
    /// <seealso cref="IPartialCompilation"/>
    /// <seealso href="@aspect-weavers"/>
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
                    modifiedSyntaxTrees.Enqueue(
                        SyntaxTreeTransformation.ReplaceTree( tree.Value, tree.Value.WithRootAndOptions( newRoot, tree.Value.Options ) ) );
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