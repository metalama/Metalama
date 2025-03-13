// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Metalama.Testing.AspectTesting
{
    internal partial class BaseTestRunner
    {
        private protected sealed class Observer : ICompileTimeCompilationBuilderObserver, ITemplateCompilerObserver, ICompilationModelObserver, ILinkerObserver
        {
            private readonly TestResult _testResult;
            private readonly ITaskRunner _taskRunner;

            public Observer( GlobalServiceProvider serviceProvider, TestResult testResult )
            {
                this._testResult = testResult;
                this._taskRunner = serviceProvider.GetRequiredService<ITaskRunner>();
            }

            public void OnCompileTimeCompilation( Compilation compilation, IReadOnlyDictionary<string, string> compileTimeToSourceMap )
                => this._taskRunner.RunSynchronously( () => this._testResult.SetCompileTimeCompilationAsync( compilation, compileTimeToSourceMap ) );

            public void OnCompileTimeCompilationEmit( ImmutableArray<Diagnostic> diagnostics )
                => this._testResult.CompileTimeCompilationDiagnostics.Report( diagnostics );

            public void OnAnnotatedSyntaxNode( SyntaxNode sourceSyntaxRoot, SyntaxNode annotatedSyntaxRoot )
            {
                var originalSyntaxTree =
                    this._testResult.SyntaxTrees
                        .Select( ( item, index ) => (item, index) )
                        .Single( x => x.item.InputSyntaxTree != null && x.item.InputSyntaxTree.FilePath == sourceSyntaxRoot.SyntaxTree.FilePath )
                        .item;

                SyntaxNode previousRoot;
                SyntaxNode previousNode;

                if ( originalSyntaxTree.AnnotatedSyntaxRoot == null )
                {
                    // This is the first time we are called.
                    previousRoot = sourceSyntaxRoot.SyntaxTree.GetRoot();
                    previousNode = sourceSyntaxRoot;
                }
                else
                {
                    // This is the second time we are called. We need to locate the node in the tree we created the previous
                    // time we were called.
                    previousRoot = originalSyntaxTree.AnnotatedSyntaxRoot;
                    Assert.True( NodeFinder.TryFindOldNodeInNewRoot( sourceSyntaxRoot, previousRoot, out previousNode ) );
                }

                originalSyntaxTree.AnnotatedSyntaxRoot = previousRoot.ReplaceNode( previousNode, annotatedSyntaxRoot );

                Assert.NotSame( originalSyntaxTree.AnnotatedSyntaxRoot, previousRoot );
            }

            public void OnInitialCompilationModelCreated( ICompilation compilation ) => this._testResult.InitialCompilationModel = compilation;

            public void OnIntermediateCompilationCreated( PartialCompilation compilation ) => this._testResult.IntermediateLinkerCompilation = compilation;
        }
    }
}